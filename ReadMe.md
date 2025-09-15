# Docker Desktop → Settings → Kubernetes → "Enable Kubernetes" açık olsun

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

helm upgrade --install apm-server elastic/apm-server