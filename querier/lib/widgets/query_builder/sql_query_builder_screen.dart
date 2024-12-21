import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart' show kIsWeb;

class SQLQueryBuilderScreen extends StatefulWidget {
  const SQLQueryBuilderScreen({super.key});

  @override
  State<SQLQueryBuilderScreen> createState() => _SQLQueryBuilderScreenState();
}

class _SQLQueryBuilderScreenState extends State<SQLQueryBuilderScreen> {
  // Pour le mock, on simule quelques tables
  final List<String> _tables = ['Users', 'Orders', 'Products', 'Categories'];
  final List<String> _selectedTables = [];
  final List<String> _selectedFields = [];
  final List<String> _conditions = [];

  @override
  Widget build(BuildContext context) {
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
          // Panneau latéral gauche - Liste des tables
          SizedBox(
            width: 250,
            child: Card(
              margin: const EdgeInsets.all(8),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Padding(
                    padding: EdgeInsets.all(8.0),
                    child: Text(
                      'Tables disponibles',
                      style: TextStyle(fontWeight: FontWeight.bold),
                    ),
                  ),
                  Expanded(
                    child: ListView.builder(
                      itemCount: _tables.length,
                      itemBuilder: (context, index) {
                        final table = _tables[index];
                        return ListTile(
                          title: Text(table),
                          trailing: IconButton(
                            icon: const Icon(Icons.add),
                            onPressed: () {
                              setState(() {
                                if (!_selectedTables.contains(table)) {
                                  _selectedTables.add(table);
                                }
                              });
                            },
                          ),
                        );
                      },
                    ),
                  ),
                ],
              ),
            ),
          ),
          // Zone principale de construction
          Expanded(
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

    return '''
SELECT 
  ${_selectedTables.map((t) => '$t.*').join(', ')}
FROM 
  ${_selectedTables.join(' \nJOIN ')}
WHERE 
  -- Conditions seront ajoutées ici
ORDER BY 
  -- Tri sera ajouté ici
''';
  }
}
