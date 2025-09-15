# **Observability Architecture**

# Elastic Stack + APM + OtelCollector + Prometheus + Grafana on Docker Desktop Kubernetes (Single Node)

## 1) Switch to Docker Desktop’s Kubernetes context and verify the node

Use Docker Desktop’s built-in single-node cluster. Switch your current context and list the nodes to confirm connectivity.

```
Docker Desktop  kubernetes ( single node cluster )

kubectl config use-context docker-desktop
kubectl get nodes
```

## 2) Add the Elastic Helm repository and refresh indexes

This ensures Helm can discover the latest Elastic charts before installation.

```
helm repo add elastic https://helm.elastic.co
helm repo update
```

## 3) Create a dedicated namespace for Elastic components

Using a separate namespace keeps Elasticsearch, Kibana, and APM Server organized and easier to manage.

```
kubectl create namespace elastic
```

## 4) Install Elasticsearch (single replica, ephemeral storage)

Deploy Elasticsearch into the `elastic` namespace with one replica, persistence disabled (non‑durable, suitable for dev), and soft anti‑affinity.

```
helm upgrade --install elastic elastic/elasticsearch -n elastic
--set replicas=1
--set persistence.enabled=false
--set antiAffinity="soft"
```

## 5) Install Kibana

Install Kibana into the same namespace. It will connect to the Elasticsearch service created by the chart (defaults apply).

```
helm upgrade --install kibana elastic/kibana  -n elastic
```

## 6) Install Elastic APM Server

Deploy the APM Server which receives APM data from your applications and forwards it to Elasticsearch.

```
helm upgrade --install apm-server elastic/apm-server -n elastic
```

## 7) Configure APM Server for HTTPS connections to Elasticsearch

Edit the APM Server configuration so it talks to Elasticsearch over HTTPS with credentials and the CA certificate path.

```
update to apm-server-apm-server-config for https connection

output.elasticsearch:
hosts: ["https://elasticsearch-master:9200"]
username: "${ELASTICSEARCH_USERNAME}"
password: "${ELASTICSEARCH_PASSWORD}"
protocol: https
ssl.certificate_authorities: /usr/share/apm-server/config/certs/ca.crt
```

## 8) Mount Elasticsearch TLS certificates into the APM Server Pod

Update the APM Server deployment to mount the certificate secret read‑only at the path referenced above.

```
update apm-server-apm-server deployment for volumeMounts and volumes

volumes

  volumes:
  	....
	- name: elasticsearch-certs
	- secret: secretName: elasticsearch-master-certs

  volumeMounts:
  	....
	- name: elasticsearch-certs
  	readOnly: true
  	mountPath: /usr/share/apm-server/config/certs
```

## 9) Verify APM Server readiness

Check the APM Server’s readiness/status endpoint. A response like the following indicates it is healthy and ready to publish.

```
check apm-server address for ready

{
"build_date": "2022-11-09T11:21:18-08:00",
"build_sha": "7cf6f555935def992ecfd9a4693771447cb51431",
"publish_ready": true,
"version": "8.5.1"
}
```

## 10) Install the OpenTelemetry Collector

Apply your Collector manifest so it can receive and export telemetry as defined in `otel-collector.yaml`.

```
kubectl apply -f otel-collector.yaml
```

## 11) Install Prometheus (minimal setup)

Create a monitoring namespace, add the community repo, and install Prometheus with your minimal values file.

```
kubectl create namespace monitoring
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

helm upgrade --install prom prometheus-community/prometheus
-n monitoring -f prometheus-values-min.yaml
```

## 12) Install Grafana

Add Grafana’s Helm repo, update indexes, and install Grafana into the `monitoring` namespace.

```
helm repo add grafana https://grafana.github.io/helm-charts
helm repo update
helm upgrade --install grafana grafana/grafana   -n monitoring 
```

### Notes

- This flow assumes Elasticsearch is using chart defaults with TLS enabled (which provides the `elasticsearch-master-certs` secret).
- Replace `${ELASTICSEARCH_USERNAME}` and `${ELASTICSEARCH_PASSWORD}` with valid credentials in your environment.
- Ensure `otel-collector.yaml` and `prometheus-values-min.yaml` are present and aligned with your ports/targets.
