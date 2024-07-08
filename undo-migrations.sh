#!/bin/bash

dotnet clean

dotnet ef migrations remove --project ../DOMAIN/DOMAIN.csproj --startup-project ../API/API.csproj --context DOMAIN.Context.OryxContext
