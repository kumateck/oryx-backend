#!/bin/bash

dotnet ef database update --project ./DOMAIN/DOMAIN.csproj --startup-project ./API/API.csproj --context DOMAIN.Context.OryxContext


