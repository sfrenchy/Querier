class DatabaseSchema {
  final List<TableDescription> tables;
  final List<ViewDescription> views;
  final List<StoredProcedureDescription> storedProcedures;
  final List<UserFunctionDescription> userFunctions;

  DatabaseSchema({
    required this.tables,
    required this.views,
    required this.storedProcedures,
    required this.userFunctions,
  });

  factory DatabaseSchema.fromJson(Map<String, dynamic> json) {
    return DatabaseSchema(
      tables: (json['tables'] as List?)
              ?.map((x) => TableDescription.fromJson(x))
              .toList() ??
          [],
      views: (json['views'] as List?)
              ?.map((x) => ViewDescription.fromJson(x))
              .toList() ??
          [],
      storedProcedures: (json['storedProcedures'] as List?)
              ?.map((x) => StoredProcedureDescription.fromJson(x))
              .toList() ??
          [],
      userFunctions: (json['userFunctions'] as List?)
              ?.map((x) => UserFunctionDescription.fromJson(x))
              .toList() ??
          [],
    );
  }
}

class TableDescription {
  final String name;
  final String schema;
  final List<ColumnDescription> columns;

  TableDescription({
    required this.name,
    required this.schema,
    required this.columns,
  });

  factory TableDescription.fromJson(Map<String, dynamic> json) {
    return TableDescription(
      name: json['name'] ?? '',
      schema: json['schema'] ?? '',
      columns: (json['columns'] as List?)
              ?.map((x) => ColumnDescription.fromJson(x))
              .toList() ??
          [],
    );
  }
}

class ViewDescription {
  final String name;
  final String schema;
  final List<ColumnDescription> columns;

  ViewDescription({
    required this.name,
    required this.schema,
    required this.columns,
  });

  factory ViewDescription.fromJson(Map<String, dynamic> json) {
    return ViewDescription(
      name: json['name'] ?? '',
      schema: json['schema'] ?? '',
      columns: (json['columns'] as List?)
              ?.map((x) => ColumnDescription.fromJson(x))
              .toList() ??
          [],
    );
  }
}

class ColumnDescription {
  final String name;
  final String dataType;
  final bool isNullable;
  final bool isPrimaryKey;
  final bool isForeignKey;
  final String? foreignKeyTable;
  final String? foreignKeyColumn;

  ColumnDescription({
    required this.name,
    required this.dataType,
    required this.isNullable,
    required this.isPrimaryKey,
    required this.isForeignKey,
    this.foreignKeyTable,
    this.foreignKeyColumn,
  });

  factory ColumnDescription.fromJson(Map<String, dynamic> json) {
    return ColumnDescription(
      name: json['name'] ?? '',
      dataType: json['dataType'] ?? '',
      isNullable: json['isNullable'] ?? false,
      isPrimaryKey: json['isPrimaryKey'] ?? false,
      isForeignKey: json['isForeignKey'] ?? false,
      foreignKeyTable: json['foreignKeyTable'],
      foreignKeyColumn: json['foreignKeyColumn'],
    );
  }
}

class StoredProcedureDescription {
  final String name;
  final String schema;
  final List<ParameterDescription> parameters;

  StoredProcedureDescription({
    required this.name,
    required this.schema,
    required this.parameters,
  });

  factory StoredProcedureDescription.fromJson(Map<String, dynamic> json) {
    return StoredProcedureDescription(
      name: json['name'] ?? '',
      schema: json['schema'] ?? '',
      parameters: (json['parameters'] as List?)
              ?.map((x) => ParameterDescription.fromJson(x))
              .toList() ??
          [],
    );
  }
}

class UserFunctionDescription {
  final String name;
  final String schema;
  final List<ParameterDescription> parameters;
  final String? returnType;

  UserFunctionDescription({
    required this.name,
    required this.schema,
    required this.parameters,
    this.returnType,
  });

  factory UserFunctionDescription.fromJson(Map<String, dynamic> json) {
    return UserFunctionDescription(
      name: json['name'] ?? '',
      schema: json['schema'] ?? '',
      parameters: (json['parameters'] as List?)
              ?.map((x) => ParameterDescription.fromJson(x))
              .toList() ??
          [],
      returnType: json['returnType'],
    );
  }
}

class ParameterDescription {
  final String name;
  final String dataType;
  final String mode;

  ParameterDescription({
    required this.name,
    required this.dataType,
    required this.mode,
  });

  factory ParameterDescription.fromJson(Map<String, dynamic> json) {
    return ParameterDescription(
      name: json['name'] ?? '',
      dataType: json['dataType'] ?? '',
      mode: json['mode'] ?? '',
    );
  }
}
