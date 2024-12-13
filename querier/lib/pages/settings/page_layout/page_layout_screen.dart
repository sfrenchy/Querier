import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/models/dynamic_row.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_event.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_state.dart';
import 'package:querier/widgets/cards/placeholder_card.dart';
import 'package:querier/widgets/dynamic_row_widget.dart';
import 'bloc/page_layout_bloc.dart';

class PageLayoutScreen extends StatelessWidget {
  final int pageId;

  const PageLayoutScreen({
    super.key,
    required this.pageId,
  });

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.pageLayout),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: l10n.newRow,
            onPressed: () => context.read<PageLayoutBloc>().add(AddRow()),
          ),
        ],
      ),
      body: Row(
        children: [
          // Panel latéral des composants
          Container(
            width: 250,
            decoration: BoxDecoration(
              border: Border(right: BorderSide(color: Colors.grey.shade300)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Text(
                    l10n.components,
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                ),
                Expanded(
                  child: ListView(
                    padding: const EdgeInsets.all(8.0),
                    children: [
                      _buildDraggableComponent('table', l10n.tableCard),
                      _buildDraggableComponent('chart', l10n.chartCard),
                      _buildDraggableComponent('metrics', l10n.metricsCard),
                    ],
                  ),
                ),
              ],
            ),
          ),
          // Zone de preview
          Expanded(
            child: BlocBuilder<PageLayoutBloc, PageLayoutState>(
              builder: (context, state) {
                if (state is PageLayoutLoading) {
                  return const Center(child: CircularProgressIndicator());
                }

                if (state is PageLayoutLoaded) {
                  return ListView.builder(
                    padding: const EdgeInsets.all(16.0),
                    itemCount: state.rows.length,
                    itemBuilder: (context, index) {
                      final row = state.rows[index];
                      return Card(
                        margin: const EdgeInsets.only(bottom: 16.0),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            ListTile(
                              title: Text('Row ${index + 1}'),
                              trailing: IconButton(
                                icon: const Icon(Icons.delete),
                                onPressed: () {
                                  context
                                      .read<PageLayoutBloc>()
                                      .add(DeleteRow(row.id));
                                },
                              ),
                            ),
                            DynamicRowWidget(
                              row: row,
                              isEditable: true,
                            ),
                          ],
                        ),
                      );
                    },
                  );
                }

                return const Center(child: Text('Error loading layout'));
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDraggableComponent(String type, String label) {
    return Draggable<String>(
      data: type,
      feedback: Card(
        child: Container(
          padding: const EdgeInsets.all(16.0),
          child: Text(label),
        ),
      ),
      child: Card(
        child: ListTile(
          leading: Icon(_getIconForType(type)),
          title: Text(label),
        ),
      ),
    );
  }

  IconData _getIconForType(String type) {
    switch (type) {
      case 'table':
        return Icons.table_chart;
      case 'chart':
        return Icons.insert_chart;
      case 'metrics':
        return Icons.dashboard;
      default:
        return Icons.widgets;
    }
  }
}
