# elastic-net-example

## Environment Variables

Create a local `.env` file in this directory with something like the following:
```
# hostname only; do not add https:// or :443
ES_ENDPOINT=abc123.es.us-central1.gcp.cloud.es.io
ES_USER=elastic
ES_PASS=

ELASTIC_APM_SERVICE_NAME=exampleService
ELASTIC_APM_ENVIRONMENT=dev
ELASTIC_APM_SERVER_URL=https://abc123.apm.us-central1.gcp.cloud.es.io:443
ELASTIC_APM_SECRET_TOKEN=
```

## Elasticsearch Mappings

Execute `./indices.sh` to setup indices, etc. on your Elasticsearch cluster.

## Running

The app depends on the environment variables being defined in the runtime environment.

## APM

127.0.0.1:5000/agg is instrumented, and will trace a call to AggregationsNoFluentWithTracing(), with output to the Elastic APM server described in the env vars.