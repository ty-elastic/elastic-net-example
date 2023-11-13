source .env

ELASTICSEARCH_URL="https://${ES_USER}:${ES_PASS}@${ES_ENDPOINT}:443"
echo $ELASTICSEARCH_URL

echo "\ncreate synonyms\n"
curl -XPUT "$ELASTICSEARCH_URL/_synonyms/example_synonyms" -H "kbn-xsrf: reporting" -H "Content-Type: application/json" -d'
{
  "synonyms_set": [
    {
      "id": "set-1",
      "synonyms": "hello, hi"
    }
  ]
}
'

echo "\ndelete example\n"
curl -XDELETE "$ELASTICSEARCH_URL/example" -H "kbn-xsrf: reporting"
echo "\ncreate example\n"
curl -XPUT "$ELASTICSEARCH_URL/example" -H "kbn-xsrf: reporting" -H "Content-Type: application/json" -d'
{
  "settings": {
    "index" : {
      "refresh_interval" : "5s"
    },
    "analysis": {
      "filter": {
        "synonyms_filter": {
          "type": "synonym_graph",
          "synonyms_set": "example_synonyms",
          "updateable": true
        }
      },
      "analyzer": {
        "example_search_analyzer": {
          "type": "custom",
          "tokenizer": "standard",
          "filter": ["lowercase", "synonyms_filter"]
        }
      }
    }
  },
  "mappings": {
    "dynamic": "strict",
    "properties": {
      "ExampleInt": {
        "type": "integer"
      }, 
      "ExampleKeyword1": {
        "type": "keyword"
      },
      "ExampleKeyword2": {
        "type": "keyword"
      },
      "ExampleSynonym": {
        "type": "text",
        "search_analyzer": "example_search_analyzer"
      },
      "ExampleText": {
        "type": "text"
      },
      "ExampleWildcard": {
        "type": "wildcard"
      },
      "exampleSubDoc.ExampleInt": {
        "type": "integer"
      },
      "exampleSubDoc.ExampleKeyword1": {
        "type": "keyword"
      },
      "exampleSubDoc.ExampleKeyword2": {
        "type": "keyword"
      },
      "ExampleSubDocArray.ExampleInt": {
        "type": "integer"
      },
      "ExampleSubDocArray.ExampleKeyword1": {
        "type": "keyword"
      },
      "ExampleSubDocArray.ExampleKeyword2": {
        "type": "keyword"
      },
      "ExampleSubDocArrayNested": {
          "type": "nested",
          "dynamic": "strict",
          "properties": {
              "ExampleInt": {
                  "type": "integer"
              },
              "ExampleKeyword1": {
                  "type": "keyword"
              },
              "ExampleKeyword2": {
                  "type": "keyword"
              }
          }
      },
      "ExampleArrayofArrays": {
          "type": "text"
      },
      "ExampleArrayOfNestedArrays": {
          "type": "nested",
          "dynamic": "strict",
          "properties": {
              "parentId": {
                  "type": "keyword"
              },
              "children": {
                "type": "nested",
                "dynamic": "strict",
                "properties": {
                  "name": {
                    "type": "text"
                  }
                }
              }
          }
      }
    }
  }
}
'