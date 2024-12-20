import 'package:flutter/material.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/widgets/color_picker_button.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

class FLLineChartCardConfig extends StatefulWidget {
  final DynamicCard card;
  final ValueChanged<Map<String, dynamic>> onConfigurationChanged;

  const FLLineChartCardConfig({
    super.key,
    required this.card,
    required this.onConfigurationChanged,
  });

  @override
  State<FLLineChartCardConfig> createState() => _FLLineChartCardConfigState();
}

class _FLLineChartCardConfigState extends State<FLLineChartCardConfig> {
  late final Map<String, dynamic> config;

  @override
  void initState() {
    super.initState();
    config = Map<String, dynamic>.from(widget.card.configuration);
    if (!config.containsKey('lines')) {
      config['lines'] = [];
      // Utiliser Future.microtask pour éviter l'erreur de setState pendant le build
      Future.microtask(() {
        widget.onConfigurationChanged(config);
      });
    }
  }

  void updateConfig(Map<String, dynamic> newConfig) {
    setState(() {
      config.clear();
      config.addAll(newConfig);
    });
    widget.onConfigurationChanged(newConfig);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return SingleChildScrollView(
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Data Source Section
            ExpansionTile(
              title: Text(l10n.dataSource),
              initiallyExpanded: true,
              children: [
                TextFormField(
                  decoration: InputDecoration(
                    labelText: l10n.apiEndpoint,
                    helperText: l10n.urlToFetchData,
                  ),
                  initialValue: (config['dataSource'] as String?) ?? '',
                  onChanged: (value) {
                    final newConfig = Map<String, dynamic>.from(config);
                    newConfig['dataSource'] = value;
                    updateConfig(newConfig);
                  },
                ),
                const SizedBox(height: 8),
                TextFormField(
                  decoration: InputDecoration(
                    labelText: l10n.refreshInterval,
                    helperText: l10n.dataRefreshFrequency,
                  ),
                  initialValue: (config['refreshInterval'] as String?) ?? '60',
                  keyboardType: TextInputType.number,
                  onChanged: (value) {
                    final newConfig = Map<String, dynamic>.from(config);
                    newConfig['refreshInterval'] = int.tryParse(value) ?? 60;
                    updateConfig(newConfig);
                  },
                ),
              ],
            ),

            // Axes Configuration
            ExpansionTile(
              title: Text(l10n.axesConfiguration),
              children: [
                // X Axis
                Text(l10n.xAxis,
                    style: const TextStyle(fontWeight: FontWeight.bold)),
                SwitchListTile(
                  title: Text(l10n.showGridLines),
                  value: config['xAxisShowGrid'] ?? true,
                  onChanged: (value) {
                    final newConfig = Map<String, dynamic>.from(config);
                    newConfig['xAxisShowGrid'] = value;
                    updateConfig(newConfig);
                  },
                ),
                TextFormField(
                  decoration: InputDecoration(
                    labelText: l10n.axisLabel,
                  ),
                  initialValue: (config['xAxisLabel'] as String?) ?? '',
                  onChanged: (value) {
                    final newConfig = Map<String, dynamic>.from(config);
                    newConfig['xAxisLabel'] = value;
                    updateConfig(newConfig);
                  },
                ),

                // Y Axis
                Text(l10n.yAxis,
                    style: const TextStyle(fontWeight: FontWeight.bold)),
                SwitchListTile(
                  title: Text(l10n.showGridLines),
                  value: config['yAxisShowGrid'] ?? true,
                  onChanged: (value) {
                    final newConfig = Map<String, dynamic>.from(config);
                    newConfig['yAxisShowGrid'] = value;
                    updateConfig(newConfig);
                  },
                ),
                TextFormField(
                  decoration: InputDecoration(
                    labelText: l10n.axisLabel,
                  ),
                  initialValue: (config['yAxisLabel'] as String?) ?? '',
                  onChanged: (value) {
                    final newConfig = Map<String, dynamic>.from(config);
                    newConfig['yAxisLabel'] = value;
                    updateConfig(newConfig);
                  },
                ),
                Row(
                  children: [
                    Expanded(
                      child: TextFormField(
                        decoration: InputDecoration(
                          labelText: l10n.minValue,
                        ),
                        initialValue: (config['yAxisMin'] as String?) ?? '',
                        keyboardType: TextInputType.number,
                        onChanged: (value) {
                          final newConfig = Map<String, dynamic>.from(config);
                          newConfig['yAxisMin'] = double.tryParse(value);
                          updateConfig(newConfig);
                        },
                      ),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: TextFormField(
                        decoration: InputDecoration(
                          labelText: l10n.maxValue,
                        ),
                        initialValue: (config['yAxisMax'] as String?) ?? '',
                        keyboardType: TextInputType.number,
                        onChanged: (value) {
                          final newConfig = Map<String, dynamic>.from(config);
                          newConfig['yAxisMax'] = double.tryParse(value);
                          updateConfig(newConfig);
                        },
                      ),
                    ),
                  ],
                ),
              ],
            ),

            // Multi-Line Configuration
            ExpansionTile(
              title: Text(l10n.linesConfiguration),
              children: [
                // Liste des lignes configurées
                ..._buildLinesList(config),

                // Bouton pour ajouter une nouvelle ligne
                ElevatedButton.icon(
                  icon: const Icon(Icons.add),
                  label: Text(l10n.addNewLine),
                  onPressed: () {
                    final newConfig = Map<String, dynamic>.from(config);
                    final lines = List<Map<String, dynamic>>.from(
                      newConfig['lines'] ?? [],
                    );
                    lines.add({
                      'id': DateTime.now().millisecondsSinceEpoch.toString(),
                      'name': 'New Line ${lines.length + 1}',
                      'color': Colors.blue.value,
                      'width': 2.0,
                      'dataField': '',
                      'showDots': true,
                      'isCurved': false,
                    });
                    newConfig['lines'] = lines;
                    updateConfig(newConfig);
                  },
                ),
              ],
            ),

            // Tooltip Configuration
            ExpansionTile(
              title: Text(l10n.tooltipSettings),
              children: [
                SwitchListTile(
                  title: Text(l10n.showTooltip),
                  value: config['showTooltip'] ?? true,
                  onChanged: (value) {
                    final newConfig = Map<String, dynamic>.from(config);
                    newConfig['showTooltip'] = value;
                    updateConfig(newConfig);
                  },
                ),
                TextFormField(
                  decoration: InputDecoration(
                    labelText: l10n.tooltipFormat,
                    helperText: l10n.tooltipFormatExample('{value}'),
                  ),
                  initialValue:
                      config['tooltipFormat']?.toString() ?? '{value}',
                  onChanged: (value) {
                    final newConfig = Map<String, dynamic>.from(config);
                    newConfig['tooltipFormat'] = value;
                    updateConfig(newConfig);
                  },
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  List<Widget> _buildLinesList(Map<String, dynamic> config) {
    final lines = List<Map<String, dynamic>>.from(config['lines'] ?? []);
    return lines.map((line) => _buildLineItem(line, config)).toList();
  }

  Widget _buildLineItem(
      Map<String, dynamic> line, Map<String, dynamic> config) {
    final l10n = AppLocalizations.of(context)!;

    return Card(
      margin: const EdgeInsets.symmetric(vertical: 8.0),
      child: Padding(
        padding: const EdgeInsets.all(8.0),
        child: Column(
          children: [
            Row(
              children: [
                Expanded(
                  child: TextFormField(
                    decoration: InputDecoration(
                      labelText: l10n.lineName,
                    ),
                    initialValue: (line['name'] as String?) ?? '',
                    onChanged: (value) {
                      final newConfig = Map<String, dynamic>.from(config);
                      final lines = List<Map<String, dynamic>>.from(
                          newConfig['lines'] ?? []);
                      final lineIndex =
                          lines.indexWhere((l) => l['id'] == line['id']);
                      if (lineIndex != -1) {
                        lines[lineIndex] = Map<String, dynamic>.from(line)
                          ..['name'] = value;
                        newConfig['lines'] = lines;
                        updateConfig(newConfig);
                      }
                    },
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.delete),
                  onPressed: () {
                    final newConfig = Map<String, dynamic>.from(config);
                    final lines = List<Map<String, dynamic>>.from(
                        newConfig['lines'] ?? []);
                    lines.removeWhere((l) => l['id'] == line['id']);
                    newConfig['lines'] = lines;
                    updateConfig(newConfig);
                  },
                ),
              ],
            ),
            TextFormField(
              decoration: InputDecoration(
                labelText: l10n.dataField,
                helperText: l10n.jsonFieldPath,
              ),
              initialValue: (line['dataField'] as String?) ?? '',
              onChanged: (value) {
                final newConfig = Map<String, dynamic>.from(config);
                final lines =
                    List<Map<String, dynamic>>.from(newConfig['lines'] ?? []);
                final lineIndex =
                    lines.indexWhere((l) => l['id'] == line['id']);
                if (lineIndex != -1) {
                  lines[lineIndex] = Map<String, dynamic>.from(line)
                    ..['dataField'] = value;
                  newConfig['lines'] = lines;
                  updateConfig(newConfig);
                }
              },
            ),
            Row(
              children: [
                Expanded(
                  child: ColorPickerButton(
                    color: Color(line['color'] ?? Colors.blue.value),
                    onColorChanged: (color) {
                      final newConfig = Map<String, dynamic>.from(config);
                      final lines = List<Map<String, dynamic>>.from(
                          newConfig['lines'] ?? []);
                      final lineIndex =
                          lines.indexWhere((l) => l['id'] == line['id']);
                      if (lineIndex != -1) {
                        lines[lineIndex] = Map<String, dynamic>.from(line)
                          ..['color'] = color!.value;
                        newConfig['lines'] = lines;
                        updateConfig(newConfig);
                      }
                    },
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: TextFormField(
                    decoration: InputDecoration(
                      labelText: l10n.lineWidth,
                    ),
                    initialValue:
                        (line['width']?.toString() as String?) ?? '2.0',
                    keyboardType: TextInputType.number,
                    onChanged: (value) {
                      final newConfig = Map<String, dynamic>.from(config);
                      final lines = List<Map<String, dynamic>>.from(
                          newConfig['lines'] ?? []);
                      final lineIndex =
                          lines.indexWhere((l) => l['id'] == line['id']);
                      if (lineIndex != -1) {
                        lines[lineIndex] = Map<String, dynamic>.from(line)
                          ..['width'] = double.tryParse(value) ?? 2.0;
                        newConfig['lines'] = lines;
                        updateConfig(newConfig);
                      }
                    },
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
