# elastic-net-example

## Environment Variables

Create a local `.env` file in this directory with something like the following:
```
# hostname only; do not add https:// or :443
ES_ENDPOINT=abc123.es.us-central1.gcp.cloud.es.io
ES_USER=elastic
ES_PASS=
```

## Elasticsearch Mappings

Execute `./indices.sh` to setup indices, etc. on your Elasticsearch cluster.

## Running

The app depends on the environment variables being defined in the runtime environment.