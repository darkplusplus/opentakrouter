{{- define "opentakrouter.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "opentakrouter.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name (include "opentakrouter.name" .) | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}

{{- define "opentakrouter.labels" -}}
app.kubernetes.io/name: {{ include "opentakrouter.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
helm.sh/chart: {{ printf "%s-%s" .Chart.Name .Chart.Version | quote }}
{{- end -}}

{{- define "opentakrouter.tlsSecretName" -}}
{{- if .Values.certManager.secretName -}}
{{- .Values.certManager.secretName -}}
{{- else if .Values.tls.secretName -}}
{{- .Values.tls.secretName -}}
{{- else -}}
{{- printf "%s-tls" (include "opentakrouter.fullname" .) -}}
{{- end -}}
{{- end -}}

{{- define "opentakrouter.provisioningTrustSecretName" -}}
{{- if .Values.provisioning.trustStore.create -}}
{{- default (printf "%s-provisioning-trust" (include "opentakrouter.fullname" .)) .Values.provisioning.trustStore.secretName -}}
{{- else if .Values.provisioning.trustStore.secretName -}}
{{- .Values.provisioning.trustStore.secretName -}}
{{- else -}}
{{- .Values.provisioning.secretName -}}
{{- end -}}
{{- end -}}

{{- define "opentakrouter.provisioningClientSecretName" -}}
{{- if .Values.provisioning.clientCertificate.create -}}
{{- default (printf "%s-provisioning-client" (include "opentakrouter.fullname" .)) .Values.provisioning.clientCertificate.secretName -}}
{{- else if .Values.provisioning.clientCertificate.secretName -}}
{{- .Values.provisioning.clientCertificate.secretName -}}
{{- else -}}
{{- .Values.provisioning.secretName -}}
{{- end -}}
{{- end -}}

{{- define "opentakrouter.publicApiScheme" -}}
{{- if .Values.provisioning.publicApiScheme -}}
{{- .Values.provisioning.publicApiScheme -}}
{{- else if .Values.ingress.publicApiScheme -}}
{{- .Values.ingress.publicApiScheme -}}
{{- else if and .Values.ingress.enabled .Values.ingress.tls -}}
https
{{- else if .Values.ingress.enabled -}}
http
{{- else if .Values.config.api.ssl -}}
https
{{- else -}}
http
{{- end -}}
{{- end -}}

{{- define "opentakrouter.publicApiPort" -}}
{{- if gt (int .Values.provisioning.publicApiPort) 0 -}}
{{- .Values.provisioning.publicApiPort -}}
{{- else if gt (int .Values.ingress.publicApiPort) 0 -}}
{{- .Values.ingress.publicApiPort -}}
{{- else if and .Values.ingress.enabled .Values.ingress.tls -}}
443
{{- else if .Values.ingress.enabled -}}
80
{{- else -}}
{{- .Values.service.apiPort -}}
{{- end -}}
{{- end -}}
