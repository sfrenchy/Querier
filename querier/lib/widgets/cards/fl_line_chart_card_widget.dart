import 'dart:async';

import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/services/data_context_service.dart';
import 'package:querier/api/api_client.dart';
import 'package:provider/provider.dart';
import 'package:querier/widgets/cards/base_card_widget.dart';

class FLLineChartCardWidget extends BaseCardWidget {
  const FLLineChartCardWidget({
    super.key,
    required DynamicCard card,
    VoidCallback? onEdit,
    VoidCallback? onDelete,
    Widget? dragHandle,
    bool isEditing = false,
    super.maxRowHeight,
  }) : super(
          card: card,
          onEdit: onEdit,
          onDelete: onDelete,
          dragHandle: dragHandle,
          isEditing: isEditing,
        );

  @override
  Widget buildCardContent(BuildContext context) {
    return _FLLineChartContent(card: card);
  }

  @override
  Widget? buildFooter(BuildContext context) => null;
}

class _FLLineChartContent extends StatefulWidget {
  final DynamicCard card;

  const _FLLineChartContent({required this.card});

  @override
  State<_FLLineChartContent> createState() => _FLLineChartContentState();
}

class _FLLineChartContentState extends State<_FLLineChartContent> {
  Map<String, dynamic>? _data;
  Timer? _refreshTimer;
  late final DataContextService _dataContextService;

  @override
  void initState() {
    super.initState();
    _dataContextService = DataContextService(context.read<ApiClient>());
    _setupRefreshTimer();
    _loadData();
  }

  void _setupRefreshTimer() {
    final refreshInterval =
        int.tryParse(widget.card.configuration['refreshInterval'] ?? '60') ??
            60;
    _refreshTimer?.cancel();
    _refreshTimer = Timer.periodic(
      Duration(seconds: refreshInterval),
      (_) => _loadData(),
    );
  }

  Future<void> _loadData() async {
    try {
      final config = widget.card.configuration;
      if (config['dataSourceType'] == 'DataSourceType.api') {
        // TODO: Implémenter le chargement depuis l'API
      } else {
        final apiClient = context.read<ApiClient>();
        final (data, _) = await apiClient.getEntityData(
          config['dataContext'] as String,
          config['entity'] as String,
          pageNumber: 1,
          pageSize: 100,
        );

        // Restructurer les données par colonne
        final Map<String, List<dynamic>> columnData = {};
        for (var row in data) {
          for (var entry in row.entries) {
            columnData.putIfAbsent(entry.key, () => []).add(entry.value);
          }
        }

        setState(() => _data = columnData);
      }
    } catch (e) {
      debugPrint('Error loading data: $e');
    }
  }

  @override
  void dispose() {
    _refreshTimer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_data == null) {
      return const Center(child: CircularProgressIndicator());
    }

    return Padding(
      padding: const EdgeInsets.all(16.0),
      child: LineChart(
        LineChartData(
          gridData: FlGridData(
            show: true,
            drawHorizontalLine:
                widget.card.configuration['yAxisShowGrid'] ?? true,
            drawVerticalLine:
                widget.card.configuration['xAxisShowGrid'] ?? true,
          ),
          titlesData: FlTitlesData(
            bottomTitles: AxisTitles(
              axisNameWidget: Text(
                widget.card.configuration['xAxisLabel'] ?? '',
              ),
            ),
            leftTitles: AxisTitles(
              axisNameWidget: Text(
                widget.card.configuration['yAxisLabel'] ?? '',
              ),
            ),
            rightTitles: const AxisTitles(
              sideTitles: SideTitles(showTitles: false),
            ),
            topTitles: const AxisTitles(
              sideTitles: SideTitles(showTitles: false),
            ),
          ),
          borderData: FlBorderData(show: true),
          minY: double.tryParse(
              widget.card.configuration['yAxisMin']?.toString() ?? ''),
          maxY: double.tryParse(
              widget.card.configuration['yAxisMax']?.toString() ?? ''),
          lineBarsData: _buildLineBarsData(),
          lineTouchData: LineTouchData(
            enabled: widget.card.configuration['showTooltip'] ?? true,
            touchTooltipData: LineTouchTooltipData(
              getTooltipItems: (touchedSpots) {
                return touchedSpots.map((spot) {
                  final format =
                      widget.card.configuration['tooltipFormat'] ?? '{value}';
                  return LineTooltipItem(
                    format.replaceAll('{value}', spot.y.toString()),
                    const TextStyle(color: Colors.white),
                  );
                }).toList();
              },
            ),
          ),
        ),
      ),
    );
  }

  List<LineChartBarData> _buildLineBarsData() {
    final lines = List<Map<String, dynamic>>.from(
      widget.card.configuration['lines'] ?? [],
    );

    return lines.map((line) {
      final dataField = line['dataField'] as String?;
      if (dataField == null || !_data!.containsKey(dataField)) {
        return LineChartBarData(spots: const []);
      }

      final values = _data![dataField] as List;

      return LineChartBarData(
        spots: values.asMap().entries.map((entry) {
          return FlSpot(
            entry.key.toDouble(), // X = index
            double.tryParse(entry.value.toString()) ?? 0, // Y = valeur
          );
        }).toList(),
        isCurved: line['isCurved'] ?? false,
        color: Color(line['color'] ?? Colors.blue.value),
        barWidth: (line['width'] ?? 2.0).toDouble(),
        isStrokeCapRound: true,
        dotData: FlDotData(
          show: line['showDots'] ?? true,
        ),
        belowBarData: BarAreaData(show: false),
      );
    }).toList();
  }
}
