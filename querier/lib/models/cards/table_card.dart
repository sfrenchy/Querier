import 'package:querier/models/dynamic_card.dart';

class TableCard extends DynamicCard {
  static const defaultColumns = [
    {'key': 'id', 'label': {'en': 'ID', 'fr': 'ID'}},
    {'key': 'name', 'label': {'en': 'Name', 'fr': 'Nom'}},
    {'key': 'email', 'label': {'en': 'Email', 'fr': 'Email'}},
    {'key': 'role', 'label': {'en': 'Role', 'fr': 'Rôle'}},
    {'key': 'status', 'label': {'en': 'Status', 'fr': 'Statut'}},
  ];

  static const defaultData = [
    {'id': '1', 'name': 'John Doe', 'email': 'john@example.com', 'role': 'Admin', 'status': 'Active'},
    {'id': '2', 'name': 'Jane Smith', 'email': 'jane@example.com', 'role': 'User', 'status': 'Active'},
    {'id': '3', 'name': 'Bob Johnson', 'email': 'bob@example.com', 'role': 'User', 'status': 'Inactive'},
    {'id': '4', 'name': 'Alice Brown', 'email': 'alice@example.com', 'role': 'Manager', 'status': 'Active'},
    {'id': '5', 'name': 'Charlie Wilson', 'email': 'charlie@example.com', 'role': 'User', 'status': 'Active'},
    {'id': '6', 'name': 'Diana Miller', 'email': 'diana@example.com', 'role': 'Admin', 'status': 'Active'},
    {'id': '7', 'name': 'Edward Davis', 'email': 'edward@example.com', 'role': 'User', 'status': 'Inactive'},
    {'id': '8', 'name': 'Fiona Clark', 'email': 'fiona@example.com', 'role': 'Manager', 'status': 'Active'},
    {'id': '9', 'name': 'George White', 'email': 'george@example.com', 'role': 'User', 'status': 'Active'},
    {'id': '10', 'name': 'Helen Green', 'email': 'helen@example.com', 'role': 'User', 'status': 'Active'},
    {'id': '11', 'name': 'Ian Taylor', 'email': 'ian@example.com', 'role': 'Admin', 'status': 'Active'},
    {'id': '12', 'name': 'Julia Adams', 'email': 'julia@example.com', 'role': 'User', 'status': 'Inactive'},
    {'id': '13', 'name': 'Kevin Moore', 'email': 'kevin@example.com', 'role': 'Manager', 'status': 'Active'},
    {'id': '14', 'name': 'Laura Hall', 'email': 'laura@example.com', 'role': 'User', 'status': 'Active'},
    {'id': '15', 'name': 'Mike Wilson', 'email': 'mike@example.com', 'role': 'User', 'status': 'Active'},
  ];

  List<Map<String, dynamic>> get columns => 
    (configuration['columns'] as List?)?.cast<Map<String, dynamic>>() ?? 
    defaultColumns;

  List<Map<String, dynamic>> get data => 
    (configuration['data'] as List?)?.cast<Map<String, dynamic>>() ?? 
    defaultData;

  const TableCard({
    required super.id,
    required super.titles,
    required super.order,
    super.gridWidth,
    super.backgroundColor,
    super.textColor,
    Map<String, dynamic>? configuration,
  }) : super(
    type: 'Table',
    configuration: configuration ?? const {
      'columns': defaultColumns,
      'data': defaultData,
    },
  );
} 