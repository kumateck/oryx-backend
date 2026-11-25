namespace DOMAIN.Entities.Base;

public sealed record ProductionBoardOrderDto(Guid DepartmentId, string DepartmentName, string Version,
    List<ProductionBoardOperationDto> Operations);
public sealed record ProductionBoardOperationDto(Guid Id, string Name, int Order);
public sealed record SaveProductionBoardOrder(string Version, List<Guid> OperationIds);
