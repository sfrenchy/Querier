import 'package:flutter/material.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/models/cards/table_card.dart';
import 'package:provider/provider.dart';
import 'package:querier/models/entity_schema.dart';
import 'package:querier/widgets/translation_manager.dart';

class TableEntityCardConfig extends StatefulWidget {
  final TableEntityCard card;
  final ValueChanged<Map<String, dynamic>> onConfigurationChanged;

  const TableEntityCardConfig({
    Key? key,
    required this.card,
    required this.onConfigurationChanged,
  }) : super(key: key);

  @override
  State<TableEntityCardConfig> createState() => _TableEntityCardConfigState();
}

class _TableEntityCardConfigState extends State<TableEntityCardConfig> {
  List<String> _contexts = [];
  List<EntitySchema> _entities = [];
  String? _selectedContext;
  String? _selectedEntity;
  bool _isLoading = true;
  List<Map<String, dynamic>> _selectedColumns = [];

  @override
  void initState() {
    super.initState();
    _loadContexts();

    // Si un contexte est déjà configuré, charger les entités
    final savedContext = widget.card.configuration['context'] as String?;
    if (savedContext != null) {
      _loadEntities(savedContext).then((_) {
        // Une fois les entités chargées, initialiser l'entité sélectionnée
        setState(() {
          _selectedEntity = widget.card.configuration['entity'] as String?;
          if (_selectedEntity != null) {
            _initializeColumns();
          }
        });
      });
    }
  }

  Future<void> _loadContexts() async {
    try {
      final apiClient = context.read<ApiClient>();
      final contexts = await apiClient.getEntityContexts();
      setState(() {
        _contexts = contexts;
        _isLoading = false;
        _selectedContext = widget.card.configuration['context'] as String?;
      });
    } catch (e) {
      setState(() => _isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error loading contexts: $e')),
        );
      }
    }
  }

  Future<void> _loadEntities(String contextTypeName) async {
    try {
      final apiClient = context.read<ApiClient>();
      final entities = await apiClient.getEntities(contextTypeName);
      setState(() {
        _entities = entities;
        _selectedEntity = widget.card.configuration['entity'] as String?;
      });
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error loading entities: $e')),
        );
      }
    }
  }

  void _initializeColumns() {
    if (_selectedEntity != null) {
      final entity = _entities.firstWhere((e) => e.name == _selectedEntity);
      final existingColumns = widget.card.configuration['columns'] as List?;

      _selectedColumns = entity.properties.map((prop) {
        Map<String, dynamic>? existingColumn;
        if (existingColumns != null) {
          try {
            existingColumn = existingColumns.firstWhere(
              (c) => c['key'] == prop.name,
            ) as Map<String, dynamic>;
          } catch (_) {
            existingColumn = null;
          }
        }

        return {
          'name': prop.name,
          'key': prop.name,
          'type': prop.type,
          'translations': existingColumn?['label'] != null
              ? Map<String, String>.from(existingColumn!['label'] as Map)
              : {'en': prop.name, 'fr': prop.name},
          'alignment':
              existingColumn?['alignment'] ?? _getDefaultAlignment(prop.type),
          'visible': existingColumn?['visible'] ?? true,
          'decimals': existingColumn?['decimals'] ??
              (_isNumericType(prop.type) ? 0 : null),
        };
      }).toList();
    }
  }

  bool _isNumericType(String type) {
    return [
      'Decimal',
      'Double',
      'Single',
      'Int32',
      'Int16',
      'Decimal?',
      'Double?',
      'Single?',
      'Int32?',
      'Int16?'
    ].contains(type);
  }

  String _getDefaultAlignment(String type) {
    switch (type) {
      case "String?":
      case 'String':
        return 'left';
      case "DateTime?":
      case "DateTime":
        return 'right';
      case "Int32?":
      case 'Int32':
      case "Int16?":
      case 'Int16':
      case "Decimal?":
      case 'Decimal':
      case "Double?":
      case 'Double':
        return 'right';
      default:
        return 'center';
    }
  }

  Widget _buildColumnConfig(Map<String, dynamic> column) {
    final l10n = AppLocalizations.of(context)!;
    return ListTile(
      key: ValueKey(column['name']),
      leading: Icon(Icons.drag_handle),
      title: Row(
        children: [
          Expanded(
            flex: 2,
            child: TranslationManager(
              translations: column['translations'],
              onTranslationsChanged: (newTranslations) {
                setState(() {
                  column['translations'] = newTranslations;
                });
                _updateColumnConfiguration();
              },
            ),
          ),
          Expanded(
            child: DropdownButton<String>(
              value: column['alignment'],
              items: [
                DropdownMenuItem(
                    value: 'left', child: Icon(Icons.format_align_left)),
                DropdownMenuItem(
                    value: 'center', child: Icon(Icons.format_align_center)),
                DropdownMenuItem(
                    value: 'right', child: Icon(Icons.format_align_right)),
              ],
              onChanged: (value) {
                setState(() {
                  column['alignment'] = value;
                });
                _updateColumnConfiguration();
              },
            ),
          ),
          if (_isNumericType(column['type']))
            SizedBox(
              width: 100,
              child: TextField(
                decoration: InputDecoration(
                  labelText: l10n.decimals,
                  isDense: true,
                ),
                keyboardType: TextInputType.number,
                controller: TextEditingController(
                    text: column['decimals']?.toString() ?? '0'),
                onChanged: (value) {
                  setState(() {
                    column['decimals'] = int.tryParse(value) ?? 0;
                  });
                  _updateColumnConfiguration();
                },
              ),
            ),
          Checkbox(
            value: column['visible'],
            onChanged: (value) {
              setState(() {
                column['visible'] = value;
              });
              _updateColumnConfiguration();
            },
          ),
        ],
      ),
      subtitle: Text('${column['type']}'),
    );
  }

  Widget _buildColumnsSection(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return ExpansionTile(
      title: Text(l10n.columns),
      children: [
        ReorderableListView(
          shrinkWrap: true,
          children: _selectedColumns
              .map((column) => _buildColumnConfig(column))
              .toList(),
          onReorder: (oldIndex, newIndex) {
            setState(() {
              if (newIndex > oldIndex) newIndex--;
              final item = _selectedColumns.removeAt(oldIndex);
              _selectedColumns.insert(newIndex, item);
            });
            _updateColumnConfiguration();
          },
        ),
      ],
    );
  }

  void _updateColumnConfiguration() {
    final newConfig = Map<String, dynamic>.from(widget.card.configuration);
    newConfig['columns'] = _selectedColumns
        .map((col) => {
              'key': col['name'],
              'label': col['translations'],
              'alignment': col['alignment'],
              'visible': col['visible'],
              'decimals': col['decimals'],
            })
        .toList();
    widget.onConfigurationChanged(newConfig);
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Section 1: Sélection de la source de données
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Source de données',
                    style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 8),
                _buildContextSelector(),
                if (_selectedContext != null) _buildEntitySelector(),
              ],
            ),
          ),
        ),

        const SizedBox(height: 16),

        // Section 2: Configuration des colonnes
        if (_selectedEntity != null)
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Colonnes',
                      style: Theme.of(context).textTheme.titleMedium),
                  const SizedBox(height: 16),
                  _buildColumnsConfiguration(),
                ],
              ),
            ),
          ),
      ],
    );
  }

  Widget _buildContextSelector() {
    final l10n = AppLocalizations.of(context)!;
    return DropdownButton<String>(
      value: _selectedContext,
      isExpanded: true,
      hint: Text(l10n.selectDataContext),
      items: _contexts
          .map(
            (context) => DropdownMenuItem(
              value: context,
              child: Text(context),
            ),
          )
          .toList(),
      onChanged: (value) {
        setState(() {
          _selectedContext = value;
          _selectedEntity = null;
          _entities.clear();
        });
        if (value != null) {
          _loadEntities(value);
          final newConfig =
              Map<String, dynamic>.from(widget.card.configuration);
          newConfig['context'] = value;
          widget.onConfigurationChanged(newConfig);
        }
      },
    );
  }

  Widget _buildEntitySelector() {
    final l10n = AppLocalizations.of(context)!;
    return DropdownButton<String>(
      value: _selectedEntity,
      isExpanded: true,
      hint: Text(l10n.selectEntity),
      items: _entities
          .map(
            (entity) => DropdownMenuItem(
              value: entity.name,
              child: Text(entity.name),
            ),
          )
          .toList(),
      onChanged: (value) {
        setState(() => _selectedEntity = value);
        if (value != null) {
          final entity = _entities.firstWhere((e) => e.name == value);
          final newConfig =
              Map<String, dynamic>.from(widget.card.configuration);
          newConfig['entity'] = value;
          newConfig['entitySchema'] = entity.toJson();

          // Initialiser les colonnes par défaut
          newConfig['columns'] = entity.properties
              .map((prop) => {
                    'key': prop.name,
                    'label': {'en': prop.name, 'fr': prop.name},
                    'alignment': _getDefaultAlignment(prop.type),
                    'visible': true,
                    'decimals': _isNumericType(prop.type) ? 0 : null,
                  })
              .toList();

          widget.onConfigurationChanged(newConfig);
          _initializeColumns(); // Mettre à jour l'interface
        }
      },
    );
  }

  Widget _buildColumnsConfiguration() {
    return ListView.separated(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      itemCount: _selectedColumns.length,
      separatorBuilder: (_, __) => const Divider(),
      itemBuilder: (context, index) {
        final column = _selectedColumns[index];
        return ExpansionTile(
          title: Text(column['name']),
          subtitle: Text('Type: ${column['type']}'),
          children: [
            Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                children: [
                  // Traductions
                  TranslationManager(
                    translations: column['translations'],
                    onTranslationsChanged: (newTranslations) =>
                        _updateColumnTranslations(index, newTranslations),
                  ),
                  const SizedBox(height: 8),

                  // Alignement
                  DropdownButtonFormField<String>(
                    value: column['alignment'],
                    decoration: const InputDecoration(labelText: 'Alignement'),
                    items: ['left', 'center', 'right']
                        .map((align) =>
                            DropdownMenuItem(value: align, child: Text(align)))
                        .toList(),
                    onChanged: (value) => _updateColumnAlignment(index, value!),
                  ),

                  // Visibilité
                  SwitchListTile(
                    title: const Text('Visible'),
                    value: column['visible'],
                    onChanged: (value) => _updateColumnVisibility(index, value),
                  ),

                  // Décimales (uniquement pour les types numériques)
                  if (_isNumericType(column['type']))
                    TextFormField(
                      decoration: const InputDecoration(labelText: 'Décimales'),
                      keyboardType: TextInputType.number,
                      initialValue: column['decimals']?.toString(),
                      onChanged: (value) =>
                          _updateColumnDecimals(index, int.tryParse(value)),
                    ),
                ],
              ),
            ),
          ],
        );
      },
    );
  }

  void _updateColumnTranslations(
      int index, Map<String, String> newTranslations) {
    setState(() {
      _selectedColumns[index]['translations'] = newTranslations;
    });
    _updateColumnConfiguration();
  }

  void _updateColumnAlignment(int index, String value) {
    setState(() {
      _selectedColumns[index]['alignment'] = value;
    });
    _updateColumnConfiguration();
  }

  void _updateColumnVisibility(int index, bool value) {
    setState(() {
      _selectedColumns[index]['visible'] = value;
    });
    _updateColumnConfiguration();
  }

  void _updateColumnDecimals(int index, int? value) {
    setState(() {
      _selectedColumns[index]['decimals'] = value;
    });
    _updateColumnConfiguration();
  }
}
