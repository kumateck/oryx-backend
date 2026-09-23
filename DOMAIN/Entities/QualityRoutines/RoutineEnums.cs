namespace DOMAIN.Entities.QualityRoutines;

public enum AnalysisType { Chemical = 0, Microbial = 1 }
public enum RoutineType { Water = 0, Environmental = 1 }
public enum RoutineOrigin { Scheduled = 0, Emergency = 1 }
public enum RoutineCadence { Monthly = 0, Quarterly = 1 }
public enum RoutineStatus { Planned = 0, InProgress = 1, Submitted = 2, Approved = 3, Rejected = 4 }
