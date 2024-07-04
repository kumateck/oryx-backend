#!/bin/bash

dotnet ef database update --project ./Veiligh.DOMAIN/Veiligh.DOMAIN.csproj --startup-project ./Veiligh.API/Veiligh.API.csproj --context Veiligh.DOMAIN.Context.VeilighContext


