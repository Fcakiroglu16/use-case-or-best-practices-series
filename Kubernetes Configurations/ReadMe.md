Docker Desktop  kubernetes ( single node cluster )

kubectl config use-context docker-desktop
kubectl get nodes

helm repo add elastic https://helm.elastic.co
helm repo update

kubectl create namespace elastic

helm upgrade --install elastic elastic/elasticsearch -n elastic
--set replicas=1
--set persistence.enabled=false
--set antiAffinity="soft"

helm upgrade --install kibana elastic/kibana  -n elastic

helm upgrade --install apm-server elastic/apm-server -n elastic

update to apm-server-apm-server-config for https connection

output.elasticsearch:
hosts: ["https://elasticsearch-master:9200"]
username: "${ELASTICSEARCH_USERNAME}"
password: "${ELASTICSEARCH_PASSWORD}"
protocol: https
ssl.certificate_authorities: /usr/share/apm-server/config/certs/ca.crt

update apm-server-apm-server deployment for volumeMounts and volumes

volumes

volumes:
....

- name: elasticsearch-certs
- secret: secretName: elasticsearch-master-certs
- volumeMounts:
  ....
- name: elasticsearch-certs
  readOnly: true
  mountPath: /usr/share/apm-server/config/certs

check apm-server address for ready

{
"build_date": "2022-11-09T11:21:18-08:00",
"build_sha": "7cf6f555935def992ecfd9a4693771447cb51431",
"publish_ready": true,
"version": "8.5.1"
}

install Prometheus

helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

helm upgrade --install prom prometheus-community/prometheus
-n monitoring -f prometheus-values-min.yaml

---------- Metrics ----------

🔹 AddAspNetCoreInstrumentation()

Collects ASP.NET Core server-side metrics:

http.server.request.duration → Histogram of request duration (latency)

http.server.active_requests → Gauge showing number of in-flight requests

http.server.request.body.size → Histogram of request body sizes

http.server.response.body.size → Histogram of response body sizes

🔹 AddHttpClientInstrumentation()

Collects outgoing HTTP client metrics (HttpClient calls):

http.client.request.duration → Histogram of request latency

http.client.active_requests → Gauge of in-progress requests

http.client.request.body.size → Histogram of request body sizes

http.client.response.body.size → Histogram of response body sizes

🔹 AddRuntimeInstrumentation()

Collects .NET runtime metrics:

GC metrics:

process.runtime.dotnet.gc.collections.count (number of collections by generation)

process.runtime.dotnet.gc.heap.size (heap size per generation)

JIT metrics:

process.runtime.dotnet.jit.il_compiled.size

process.runtime.dotnet.jit.methods_compiled.count

Thread pool metrics:

process.runtime.dotnet.thread_pool.threads.count

process.runtime.dotnet.thread_pool.queue.length

🔹 AddProcessInstrumentation()

Collects OS process-level metrics:

process.cpu.time (total CPU time consumed by the process)

process.memory.usage (current process memory usage)

process.memory.virtual (virtual memory size)

process.threads (thread count of the process)

process.open_file_descriptors (if supported on OS)
