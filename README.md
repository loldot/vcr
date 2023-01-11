# vcr
A command line utility to parse HAR-files and play with Http Archive files.

## Commands
- replay: Replay requests from .har file
- :warning: (wip) verify: Check that the servers current responses matches those stored in the .har file
- :warning: (wip) serve: Run a local server which uses the responses stored in the .har file to respond to requests.


## Example Usage
To start serving the example .har file from yr.no move to the vcr.console directory and run:
```bash
dotnet run -- serve --file .\yr.har
```
This should start a server running on port 9000 which will reply to relative urls in exactly the same way as the original server.

### Result:

```bash
curl http://localhost:9000/api/v0/locations/1-72837?language=nb | jq -C
```

``` json 
{
  "category": {
    "id": "CB09",
    "name": "By"
  },
  "id": "1-72837",
  "name": "Oslo",
  "position": {
    "lat": 59.91273,
    "lon": 10.74609
  },
  "elevation": 10,
  "coastalPoint": {
    "lat": 59.89846,
    "lon": 10.7408
  },
  "timeZone": "Europe/Oslo",
  "urlPath": "Norge/Oslo/Oslo/Oslo",
  "country": {
    "id": "NO",
    "name": "Norge"
  },
  "region": {
    "id": "NO/03",
    "name": "Oslo"
  },
  "subregion": {
    "id": "NO/03/0301",
    "name": "Oslo"
  },
  "isInOcean": false,
  "_links": {
    "self": {
      "href": "/api/v0/locations/1-72837"
    },
    "celestialevents": {
      "href": "/api/v0/locations/1-72837/celestialevents"
    },
    "forecast": {
      "href": "/api/v0/locations/1-72837/forecast"
    },
    "cameras": {
      "href": "/api/v0/locations/1-72837/cameras"
    },
    "now": {
      "href": "/api/v0/locations/1-72837/forecast/now"
    },
    "coast": {
      "href": "/api/v0/locations/1-72837/forecast/coast"
    },
    "tide": {
      "href": "/api/v0/locations/1-72837/tide"
    },
    "mapfeature": {
      "href": "/api/v0/locations/1-72837/mapfeature"
    },
    "notifications": {
      "href": "/api/v0/locations/1-72837/notifications"
    },
    "extremeforecasts": {
      "href": "/api/v0/locations/1-72837/notifications/extreme"
    },
    "currenthour": {
      "href": "/api/v0/locations/1-72837/forecast/currenthour"
    },
    "observations": [
      {
        "href": "/api/v0/locations/1-72837/observations"
      },
      {
        "href": "/api/v0/locations/1-72837/observations/nearby"
      },
      {
        "href": "/api/v0/locations/1-72837/observations/year"
      },
      {
        "href": "/api/v0/locations/1-72837/observations/month"
      },
      {
        "href": "/api/v0/locations/1-72837/observations/day"
      },
      {
        "href": "/api/v0/locations/1-72837/observations/yyyy-MM-dd"
      }
    ],
    "watertemperatures": {
      "href": "/api/v0/locations/1-72837/nearestwatertemperatures"
    },
    "airqualityforecast": {
      "href": "/api/v0/locations/1-72837/airqualityforecast"
    },
    "auroraforecast": {
      "href": "/api/v0/locations/1-72837/auroraforecast"
    }
  }
}
``` 