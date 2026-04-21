# Certificate Runbook

This runbook describes the certificate material `opentakrouter` needs and how to create and publish it.

## Certificate Roles

`opentakrouter` uses three distinct certificate artifacts:

1. Server TLS certificate and key
Used by:
- `server.api.cert`
- `server.api.key`
- `server.links[*].cert`
- `server.links[*].key`

2. Provisioning trust bundle
Used by:
- `server.provisioning.trustStoreCertificate`

3. Provisioning client identity bundle
Used by:
- `server.provisioning.clientCertificate`
- `server.provisioning.clientCertificatePassword`

Do not collapse these into one concept. The server TLS keypair identifies the router. The provisioning trust bundle tells the EUD what to trust. The provisioning client bundle is the identity imported onto the EUD.

## Recommended Production Shape

For a public deployment such as `opentakrouter.vectaryon.com`:

- server TLS:
  Use a public CA-issued leaf certificate, for example Let's Encrypt
- provisioning trust bundle:
  Use a prebuilt `.p12` containing the certificates you want the EUD to import
- provisioning client identity:
  Use a separate client `.p12`

If you are using Let's Encrypt for the server, the most stable provisioning trust bundle is usually a PKCS#12 containing:

- the Let's Encrypt intermediate currently in use
- the root your devices should trust, usually `ISRG Root X1`

## Runbook A: Public CA Server Cert Plus Separate Enrollment Material

This is the common internet-facing deployment shape.

### 1. Obtain the server leaf certificate and private key

If cert-manager manages the live server cert, you already have:

- `tls.crt`
- `tls.key`

If you need local files from Kubernetes for inspection:

```bash
kubectl -n opentakrouter get secret opentakrouter-tls -o jsonpath='{.data.tls\.crt}' | base64 -d > tls.crt
kubectl -n opentakrouter get secret opentakrouter-tls -o jsonpath='{.data.tls\.key}' | base64 -d > tls.key
```

Verify the leaf:

```bash
openssl x509 -in tls.crt -noout -subject -issuer -dates -ext subjectAltName
```

### 2. Build the provisioning trust bundle

Fetch the certificates you want to place in the EUD trust store. Example with Let's Encrypt R13 and ISRG Root X1:

```bash
curl -fsSL https://letsencrypt.org/certs/2024/r13.pem -o r13.pem
curl -fsSL https://letsencrypt.org/certs/isrgrootx1.pem -o isrg-root-x1.pem
```

Create a PKCS#12 trust bundle:

```bash
openssl pkcs12 -export \
  -nokeys \
  -out provisioning-trust.p12 \
  -in r13.pem \
  -certfile isrg-root-x1.pem \
  -passout pass:'change-me-trust-password'
```

Inspect the resulting bundle:

```bash
openssl pkcs12 -in provisioning-trust.p12 -nokeys -info -passin pass:'change-me-trust-password'
```

### 3. Generate a client identity bundle for EUD onboarding

Generate a client key and certificate request:

```bash
openssl genrsa -out eud-client.key 2048
openssl req -new -key eud-client.key -out eud-client.csr -subj "/CN=otr-eud-client"
```

Sign it with your enrollment CA:

```bash
openssl x509 -req \
  -in eud-client.csr \
  -CA enrollment-ca.crt \
  -CAkey enrollment-ca.key \
  -CAcreateserial \
  -out eud-client.crt \
  -days 365 \
  -sha256
```

Export the client identity as PKCS#12:

```bash
openssl pkcs12 -export \
  -out client.p12 \
  -inkey eud-client.key \
  -in eud-client.crt \
  -certfile enrollment-ca.crt \
  -passout pass:'change-me-client-password'
```

Inspect it:

```bash
openssl pkcs12 -in client.p12 -info -passin pass:'change-me-client-password'
```

## Runbook B: Fully Private CA Lab Or Small Deployment

This is useful for local testing and private deployments where you control both the server and the EUD trust anchor.

### 1. Create a CA

```bash
openssl genrsa -out local-ca.key 4096
openssl req -x509 -new -nodes \
  -key local-ca.key \
  -sha256 \
  -days 3650 \
  -out local-ca.crt \
  -subj "/CN=OpenTAKRouter Local CA"
```

### 2. Create a server certificate for the router

```bash
cat > server-san.cnf <<'EOF'
[req]
distinguished_name=req_distinguished_name
req_extensions=req_ext
prompt=no

[req_distinguished_name]
CN=opentakrouter.vectaryon.com

[req_ext]
subjectAltName=@alt_names

[alt_names]
DNS.1=opentakrouter.vectaryon.com
EOF
```

```bash
openssl genrsa -out tls.key 2048
openssl req -new -key tls.key -out tls.csr -config server-san.cnf
openssl x509 -req \
  -in tls.csr \
  -CA local-ca.crt \
  -CAkey local-ca.key \
  -CAcreateserial \
  -out tls.crt \
  -days 825 \
  -sha256 \
  -extensions req_ext \
  -extfile server-san.cnf
```

### 3. Create a client identity bundle

```bash
openssl genrsa -out client.key 2048
openssl req -new -key client.key -out client.csr -subj "/CN=otr-eud-client"
openssl x509 -req \
  -in client.csr \
  -CA local-ca.crt \
  -CAkey local-ca.key \
  -CAcreateserial \
  -out client.crt \
  -days 365 \
  -sha256
openssl pkcs12 -export \
  -out client.p12 \
  -inkey client.key \
  -in client.crt \
  -certfile local-ca.crt \
  -passout pass:'change-me-client-password'
```

### 4. Create a trust-store PKCS#12 from the CA cert

```bash
openssl pkcs12 -export \
  -nokeys \
  -out provisioning-trust.p12 \
  -in local-ca.crt \
  -passout pass:'change-me-trust-password'
```

## Kubernetes Secret Publishing

The current Helm chart supports:

- one TLS secret for server identity
- one provisioning trust secret
- one provisioning client secret
- separate password secrets referenced by key

### 1. Create the server TLS secret

```bash
kubectl -n opentakrouter create secret tls opentakrouter-tls \
  --cert=tls.crt \
  --key=tls.key
```

### 2. Create the provisioning artifact secrets

Trust bundle:

```bash
kubectl -n opentakrouter create secret generic opentakrouter-provisioning-trust \
  --from-file=trust.p12=provisioning-trust.p12
```

Client identity:

```bash
kubectl -n opentakrouter create secret generic opentakrouter-provisioning-client \
  --from-file=client.p12=client.p12
```

### 3. Create password secrets

Trust bundle password:

```bash
kubectl -n opentakrouter create secret generic opentakrouter-provisioning-trust-password \
  --from-literal=password='change-me-trust-password'
```

Client identity password:

```bash
kubectl -n opentakrouter create secret generic opentakrouter-provisioning-client-password \
  --from-literal=password='change-me-client-password'
```

## Helm Values Example

Example values for an ingress-published deployment:

```yaml
config:
  publicEndpoint: opentakrouter.vectaryon.com

tls:
  secretName: opentakrouter-tls

ingress:
  enabled: true
  publicApiScheme: https
  publicApiPort: 443
  hosts:
    - host: opentakrouter.vectaryon.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - hosts:
        - opentakrouter.vectaryon.com

provisioning:
  enabled: true
  trustStore:
    secretName: opentakrouter-provisioning-trust
    secretKey: trust.p12
    fileName: trust.p12
  trustStorePasswordSecretName: opentakrouter-provisioning-trust-password
  trustStorePasswordSecretKey: password
  clientCertificate:
    secretName: opentakrouter-provisioning-client
    secretKey: client.p12
    fileName: client.p12
  clientCertificatePasswordSecretName: opentakrouter-provisioning-client-password
  clientCertificatePasswordSecretKey: password
```

If you are migrating an older values file, the chart still accepts literal `provisioning.trustStorePassword` and `provisioning.clientCertificatePassword` values. Prefer the secret-based settings above for production.

## Verification Checklist

Check the live server chain on the TAK listener:

```bash
openssl s_client -connect opentakrouter.vectaryon.com:8089 -servername opentakrouter.vectaryon.com -showcerts </dev/null
```

Check the live provisioning config:

```bash
kubectl -n opentakrouter get configmap opentakrouter-opentakrouter-config -o jsonpath='{.data.opentakrouter\.json}'
```

Check that the generated trust bundle contains the expected certificates:

```bash
openssl pkcs12 -in provisioning-trust.p12 -nokeys -info -passin pass:'change-me-trust-password'
```

Check that the generated client bundle is readable:

```bash
openssl pkcs12 -in client.p12 -info -passin pass:'change-me-client-password'
```

## Operational Rules

- `server.public_endpoint` must match what the EUD connects to on `8089`
- the server TLS cert SAN must include that hostname
- `server.provisioning.publicApiScheme` and `server.provisioning.publicApiPort` must match the public download URL for the ZIP if devices or operators fetch it remotely
- do not put private-key passphrases directly into Helm values or ConfigMaps
- prefer externally managed secrets in production
- if you use Let's Encrypt, do not assume the current intermediate is permanent; package a trust bundle intentionally
