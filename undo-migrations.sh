#!/bin/bash

dotnet clean

dotnet ef migrations remove --project ../Veiligh.DOMAIN/Veiligh.DOMAIN.csproj --startup-project ../Veiligh.API/Veiligh.API.csproj --context Veiligh.DOMAIN.Context.VeilighContext
