#!/bin/zsh

function failed() {
    local error=${1:-Undefined error}
    echo "Failed: $error" >&2
    exit 1
}

function check_kubectl_installed() {
  if ! command -v kubectl &> /dev/null
  then
    failed "kubectl could not be found"
  fi
}

function wait_for_seconds() {
  local seconds=$1
  local message=$2
  echo -n "$message "
  while [ $seconds -gt 0 ]; do
    sleep 1
    echo -n .
    seconds=$((seconds-1))
  done
  echo
}


########### Main ###########

check_kubectl_installed

if [ "$#" -ne 5 ]; then
  echo "Usage: $0 <container_name> <environment> <deploy_namespace> <kubeconfig_file_path> <tld>"
  echo "Example: $0 sparrow-cert DEV my-k8s-namespace ~/.kube/config mydomain.com"
  failed "number of arguments is incorrect"
fi
## arguments
CONTAINER_NAME=$1
ENVIRONMENT=$2
DEPLOY_NAMESPACE=$3
KUBECONFIG_FILE=$4
TLD=$5

echo CONTAINER_NAME=$CONTAINER_NAME
echo ENVIRONMENT=$ENVIRONMENT
echo DEPLOY_NAMESPACE=$DEPLOY_NAMESPACE
echo KUBECONFIG_FILE=$KUBECONFIG_FILE
echo TLD=$TLD

# Set yaml file prefix
if [ -z "$ENVIRONMENT" ]; then
  YML_FILE_PREFIX=$CONTAINER_NAME
else
  YML_FILE_PREFIX=$CONTAINER_NAME-$ENVIRONMENT
fi


# Add hostPath for all nodes in kube-system namespace to create {hostPath}/keystore/ directory
echo "Adding hostPath for all nodes"
kubectl apply -f $YML_FILE_PREFIX-daemonset.yml --kubeconfig $KUBECONFIG_FILE -n kube-system
wait_for_seconds 10 "Waiting for DaemonSet to be applied"
kubectl get pods -l app=create-cert-dir --kubeconfig $KUBECONFIG_FILE -n kube-system
echo "Deleting DaemonSet ..."
kubectl delete daemonset create-cert-dir --kubeconfig $KUBECONFIG_FILE -n kube-system


## Apply k8s resources (Namespace, Config Secret)
echo "Applying k8s Namespace and Config JSON Secret for $CONTAINER_NAME in (env='$ENVIRONMENT')"
kubectl apply -f $YML_FILE_PREFIX-namespace.yml --kubeconfig $KUBECONFIG_FILE -n $DEPLOY_NAMESPACE
kubectl apply -f $YML_FILE_PREFIX-config-secret.yml --kubeconfig $KUBECONFIG_FILE -n $DEPLOY_NAMESPACE


# Check if $TLD-cert.pfx and $TLD-privkey.pem exist
# List files to be created as Secrets
echo "Checking if required files exist"
CERT_FILES=( $TLD-cert.pfx $TLD-privkey.pem $TLD.staging-cert.pfx $TLD.staging-privkey.pem )
FROM_FILES=()
for file in $CERT_FILES; do
  if [ ! -f /etc/sparrow-cert/config/$file ]; then
    failed "File /etc/sparrow-cert/config/$file does not exist"
  fi
  echo "File /etc/sparrow-cert/config/$file exists"
  FROM_FILES+=(--from-file=/etc/sparrow-cert/config/"$file")
done
kubectl create secret generic sparrow-secret "${FROM_FILES[@]}" --kubeconfig "$KUBECONFIG_FILE" -n "$DEPLOY_NAMESPACE"


# Apply Network (Service and Ingress)
kubectl apply -f $YML_FILE_PREFIX-network.yml --kubeconfig $KUBECONFIG_FILE -n $DEPLOY_NAMESPACE
wait_for_seconds 5 "Waiting for network policy to be applied"


## Apply CronJob
kubectl apply -f $YML_FILE_PREFIX-cronjob.yml --kubeconfig $KUBECONFIG_FILE -n $DEPLOY_NAMESPACE