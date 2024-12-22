import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:querier/models/db_connection.dart';
import 'package:querier/models/db_schema.dart';
import 'package:querier/api/api_client.dart';
import 'package:reorderables/reorderables.dart';
import 'package:resizable_widget/resizable_widget.dart';
import '../resizable_panel.dart';

class SQLQueryBuilderScreen extends StatefulWidget {
  final DBConnection database;
  final ApiClient apiClient;

  const SQLQueryBuilderScreen({
    super.key,
    required this.database,
    required this.apiClient,
  });

  @override
  State<SQLQueryBuilderScreen> createState() => _SQLQueryBuilderScreenState();
}

class _SQLQueryBuilderScreenState extends State<SQLQueryBuilderScreen> {
  DatabaseSchema? _schema;
  bool _loading = true;
  String? _error;

  final List<String> _selectedTables = [];
  final List<String> _selectedFields = [];
  final List<String> _conditions = [];

  @override
  void initState() {
    super.initState();
    _loadDatabaseSchema();
  }

  Future<void> _loadDatabaseSchema() async {
    try {
      setState(() {
        _loading = true;
        _error = null;
      });

      final schema =
          await widget.apiClient.getDatabaseSchema(widget.database.id);

      print('Schema loaded:');
      print('Tables: ${schema.tables.length}');
      print('Views: ${schema.views.length}');
      print('Stored Procedures: ${schema.storedProcedures.length}');
      print('User Functions: ${schema.userFunctions.length}');

      setState(() {
        _schema = schema;
        _loading = false;
      });
    } catch (e, stackTrace) {
      print('Error loading schema: $e');
      print('Stack trace: $stackTrace');
      setState(() {
        _error = e.toString();
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Scaffold(
        body: Center(
          child: CircularProgressIndicator(),
        ),
      );
    }

    if (_error != null) {
      return Scaffold(
        body: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text('Erreur: $_error'),
              ElevatedButton(
                onPressed: _loadDatabaseSchema,
                child: const Text('Réessayer'),
              ),
            ],
          ),
        ),
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: const Text('Constructeur de requêtes SQL'),
        actions: [
          IconButton(
            icon: const Icon(Icons.play_arrow),
            onPressed: () {
              // TODO: Exécuter la requête
            },
            tooltip: 'Exécuter la requête',
          ),
          IconButton(
            icon: const Icon(Icons.save),
            onPressed: () {
              // TODO: Sauvegarder la requête
            },
            tooltip: 'Sauvegarder la requête',
          ),
        ],
      ),
      body: Row(
        children: [
          ResizablePanel(
            key: const ValueKey('left_panel'),
            child: Card(
              margin: const EdgeInsets.all(8),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Padding(
                    padding: EdgeInsets.all(8.0),
                    child: Text(
                      'Objects',
                      style: TextStyle(fontWeight: FontWeight.bold),
                    ),
                  ),
                  Expanded(
                    child: ListView(
                      children: [
                        // Tables Section
                        ExpansionTile(
                          title: const Text('Tables'),
                          initiallyExpanded: true,
                          children: _schema?.tables
                                  .map((table) => ListTile(
                                        title: Text(table.name),
                                        subtitle: Text(table.schema),
                                        trailing: IconButton(
                                          icon: const Icon(Icons.add),
                                          onPressed: () {
                                            setState(() {
                                              if (!_selectedTables
                                                  .contains(table.name)) {
                                                _selectedTables.add(table.name);
                                              }
                                            });
                                          },
                                        ),
                                      ))
                                  .toList() ??
                              [],
                        ),

                        // Views Section
                        ExpansionTile(
                          title: const Text('Views'),
                          children: _schema?.views
                                  .map((view) => ListTile(
                                        title: Text(view.name),
                                        subtitle: Text(view.schema),
                                        trailing: IconButton(
                                          icon: const Icon(Icons.add),
                                          onPressed: () {
                                            setState(() {
                                              if (!_selectedTables
                                                  .contains(view.name)) {
                                                _selectedTables.add(view.name);
                                              }
                                            });
                                          },
                                        ),
                                      ))
                                  .toList() ??
                              [],
                        ),

                        // Stored Procedures Section
                        ExpansionTile(
                          title: const Text('Stored Procedures'),
                          children: _schema?.storedProcedures
                                  .map((proc) => ListTile(
                                        title: Text(proc.name),
                                        subtitle: Text(proc.schema),
                                        trailing: IconButton(
                                          icon: const Icon(Icons.add),
                                          onPressed: () {
                                            // TODO: Implémenter la logique pour les procédures stockées
                                          },
                                        ),
                                      ))
                                  .toList() ??
                              [],
                        ),

                        // User Functions Section
                        ExpansionTile(
                          title: const Text('User Functions'),
                          children: _schema?.userFunctions
                                  .map((func) => ListTile(
                                        title: Text(func.name),
                                        subtitle: Text(func.schema),
                                        trailing: IconButton(
                                          icon: const Icon(Icons.add),
                                          onPressed: () {
                                            // TODO: Implémenter la logique pour les fonctions
                                          },
                                        ),
                                      ))
                                  .toList() ??
                              [],
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          // Zone principale de construction
          Expanded(
            key: const ValueKey('main_content'),
            child: Column(
              children: [
                // Zone de visualisation des tables sélectionnées
                Expanded(
                  child: Card(
                    margin: const EdgeInsets.all(8),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Padding(
                          padding: EdgeInsets.all(8.0),
                          child: Text(
                            'Diagramme des relations',
                            style: TextStyle(fontWeight: FontWeight.bold),
                          ),
                        ),
                        Expanded(
                          child: Stack(
                            children: [
                              // Ici on afficherait le diagramme interactif
                              // Pour le mock, on affiche juste les tables sélectionnées
                              ..._selectedTables.map((table) => Positioned(
                                    left: 50.0 * _selectedTables.indexOf(table),
                                    top: 50.0 * _selectedTables.indexOf(table),
                                    child: Card(
                                      color: Colors.blue.shade100,
                                      child: Padding(
                                        padding: const EdgeInsets.all(8.0),
                                        child: Column(
                                          crossAxisAlignment:
                                              CrossAxisAlignment.start,
                                          children: [
                                            Text(
                                              table,
                                              style: const TextStyle(
                                                  fontWeight: FontWeight.bold),
                                            ),
                                            const Text('id: int'),
                                            const Text('name: string'),
                                            // Simuler quelques champs
                                          ],
                                        ),
                                      ),
                                    ),
                                  )),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                // Zone de construction de la requête
                Expanded(
                  child: Card(
                    margin: const EdgeInsets.all(8),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Padding(
                          padding: EdgeInsets.all(8.0),
                          child: Text(
                            'Requête SQL',
                            style: TextStyle(fontWeight: FontWeight.bold),
                          ),
                        ),
                        Expanded(
                          child: Container(
                            margin: const EdgeInsets.all(8),
                            padding: const EdgeInsets.all(16),
                            decoration: BoxDecoration(
                              color: const Color(0xFF282C34),
                              borderRadius: BorderRadius.circular(4),
                              border: Border.all(
                                color: Colors.grey.shade700,
                                width: 1,
                              ),
                            ),
                            child: SelectableText(
                              _buildMockQuery(),
                              style: const TextStyle(
                                fontFamily: 'ui-monospace',
                                fontSize: 14,
                                color: Colors.white,
                                height: 1.5,
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
          // Panneau latéral droit - Options de requête
          SizedBox(
            key: const ValueKey('right_panel'),
            width: 250,
            child: Card(
              margin: const EdgeInsets.all(8),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Padding(
                    padding: EdgeInsets.all(8.0),
                    child: Text(
                      'Options de requête',
                      style: TextStyle(fontWeight: FontWeight.bold),
                    ),
                  ),
                  ListTile(
                    title: const Text('Champs'),
                    trailing: IconButton(
                      icon: const Icon(Icons.add),
                      onPressed: () {
                        // TODO: Ajouter un champ
                      },
                    ),
                  ),
                  ListTile(
                    title: const Text('Conditions'),
                    trailing: IconButton(
                      icon: const Icon(Icons.add),
                      onPressed: () {
                        // TODO: Ajouter une condition
                      },
                    ),
                  ),
                  ListTile(
                    title: const Text('Tri'),
                    trailing: IconButton(
                      icon: const Icon(Icons.sort),
                      onPressed: () {
                        // TODO: Configurer le tri
                      },
                    ),
                  ),
                  ListTile(
                    title: const Text('Groupement'),
                    trailing: IconButton(
                      icon: const Icon(Icons.group_work),
                      onPressed: () {
                        // TODO: Configurer le groupement
                      },
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  String _buildMockQuery() {
    if (_selectedTables.isEmpty) {
      return '-- Sélectionnez des tables pour construire la requête';
    }

    final selectedTablesInfo = _selectedTables.map((tableName) {
      final tableOrView = _schema?.tables.firstWhere(
        (t) => t.name == tableName,
        orElse: () => TableDescription(
          name: tableName,
          schema: 'dbo',
          columns: [],
        ),
      );
      return '${tableOrView?.schema ?? 'dbo'}.$tableName';
    }).toList();

    return '''
SELECT 
  ${selectedTablesInfo.map((t) => '$t.*').join(', ')}
FROM 
  ${selectedTablesInfo.join(' \nJOIN ')}
WHERE 
  -- Conditions seront ajoutées ici
ORDER BY 
  -- Tri sera ajouté ici
''';
  }
}
