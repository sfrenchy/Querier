import 'dart:async';
import 'dart:math';

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
    return _FLLineChartContent(
      card: card,
      onBuildFooter: buildFooter,
    );
  }

  @override
  Widget? buildFooter(BuildContext context) => null;
}

class _FLLineChartContent extends StatefulWidget {
  final DynamicCard card;
  final Widget? Function(BuildContext)? onBuildFooter;

  const _FLLineChartContent({
    required this.card,
    this.onBuildFooter,
  });

  @override
  State<_FLLineChartContent> createState() => _FLLineChartContentState();
}

class _FLLineChartContentState extends State<_FLLineChartContent> {
  Map<String, dynamic>? _data;
  Timer? _refreshTimer;
  late final DataContextService _dataContextService;
  final _paginationController = StreamController<(int, int)>.broadcast();
  static const int _defaultPageSize = 100;
  int? _totalItems;
  int _currentPage = 1;

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

        // Utiliser la pagination si elle est activée
        final bool isPaginated = config['pagination'] ?? false;
        final int pageSize =
            isPaginated ? (config['pageSize'] as int? ?? _defaultPageSize) : 0;
        final int pageNumber = isPaginated ? _currentPage : 0;

        final (data, total) = await apiClient.getEntityData(
          config['dataContext'] as String,
          config['entity'] as String,
          pageNumber: pageNumber,
          pageSize: pageSize,
          orderBy: config['orderBy'] as String? ?? '',
        );

        // Mettre à jour le total et envoyer l'état de pagination
        _totalItems = total;
        if (isPaginated) {
          _paginationController.add((_currentPage, total));
        }

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
    _paginationController.close();
    _refreshTimer?.cancel();
    super.dispose();
  }

  Widget? _buildFooter(BuildContext context) {
    if (!(widget.card.configuration['pagination'] as bool? ?? false)) {
      return null;
    }

    return StreamBuilder<(int, int)>(
      stream: _paginationController.stream,
      builder: (context, snapshot) {
        if (!snapshot.hasData) return const SizedBox.shrink();

        final (currentPage, totalItems) = snapshot.data!;
        final pageSize =
            widget.card.configuration['pageSize'] as int? ?? _defaultPageSize;
        final startIndex = (currentPage - 1) * pageSize + 1;
        final endIndex = min(startIndex + pageSize - 1, totalItems);
        final totalPages = (totalItems / pageSize).ceil();

        return Container(
          padding: const EdgeInsets.all(8.0),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text('$startIndex-$endIndex sur $totalItems'),
              IconButton(
                icon: const Icon(Icons.chevron_left),
                onPressed: currentPage > 1
                    ? () {
                        setState(() => _currentPage--);
                        _loadData();
                      }
                    : null,
              ),
              DropdownButton<int>(
                value: currentPage,
                isDense: true,
                items: List.generate(totalPages, (index) => index + 1)
                    .map((page) => DropdownMenuItem(
                          value: page,
                          child: Text('$page / $totalPages'),
                        ))
                    .toList(),
                onChanged: (newPage) {
                  if (newPage != null) {
                    setState(() => _currentPage = newPage);
                    _loadData();
                  }
                },
              ),
              IconButton(
                icon: const Icon(Icons.chevron_right),
                onPressed: currentPage < totalPages
                    ? () {
                        setState(() => _currentPage++);
                        _loadData();
                      }
                    : null,
              ),
            ],
          ),
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Expanded(child: _buildChart()),
        if (_buildFooter(context) != null) _buildFooter(context)!,
      ],
    );
  }

  Widget _buildChart() {
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
