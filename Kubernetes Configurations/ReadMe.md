Docker Desktop  kubernetes ( single node cluster )

kubectl config use-context docker-desktop
kubectl get nodes

helm repo add elastic https://helm.elastic.co
helm repo update

kubectl create namespace elastic


helm upgrade --install elastic elastic/elasticsearch -n elastic \
  --set replicas=1 \
  --set persistence.enabled=false \
  --set antiAffinity="soft"

helm upgrade --install kibana elastic/kibana  -n elastic

helm upgrade --install apm-server elastic/apm-server -n elastic








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