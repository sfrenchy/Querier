import 'package:flutter/material.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'dart:async';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/models/dynamic_row.dart';
import 'package:querier/widgets/draggable_row.dart';
import 'package:querier/widgets/row_properties_dialog.dart';
import 'bloc/dynamic_page_layout_bloc.dart';
import 'bloc/dynamic_page_layout_event.dart';
import 'bloc/dynamic_page_layout_state.dart';

class DynamicPageLayoutScreen extends StatefulWidget {
  final int pageId;

  const DynamicPageLayoutScreen({
    super.key,
    required this.pageId,
  });

  @override
  State<DynamicPageLayoutScreen> createState() =>
      _DynamicPageLayoutScreenState();
}

class _DynamicPageLayoutScreenState extends State<DynamicPageLayoutScreen> {
  bool _isPinned = false;
  bool _isExpanded = false;
  Timer? _collapseTimer;

  void _startCollapseTimer() {
    _collapseTimer?.cancel();
    if (!_isPinned) {
      _collapseTimer = Timer(const Duration(seconds: 3), () {
        if (mounted && !_isPinned) {
          setState(() {
            _isExpanded = false;
          });
        }
      });
    }
  }

  @override
  void dispose() {
    _collapseTimer?.cancel();
    super.dispose();
  }

  Widget _buildMainContent(BuildContext context, DynamicPageLayoutState state) {
    final l10n = AppLocalizations.of(context)!;

    if (state is DynamicPageLayoutLoaded) {
      return ListView(
        children: [
          ...state.rows.asMap().entries.map((entry) {
            return DragTarget<DynamicRow>(
              onWillAccept: (data) {
                print(
                    'onWillAccept: data=${data?.id}, targetRow=${entry.value.id}');
                return data != null;
              },
              onAccept: (data) {
                print(
                    'onAccept: sourceRow=${data.id}, targetRow=${entry.value.id}');
                final rows = state.rows;
                final oldIndex = rows.indexOf(data);
                final newIndex = rows.indexOf(entry.value);
                print('Indices: oldIndex=$oldIndex, newIndex=$newIndex');

                if (oldIndex != newIndex) {
                  final rowIds = rows.map((r) => r.id).toList();
                  print('Before reorder: rowIds=$rowIds');
                  final id = rowIds.removeAt(oldIndex);
                  rowIds.insert(newIndex, id);
                  print('After reorder: rowIds=$rowIds');

                  context.read<DynamicPageLayoutBloc>().add(
                        ReorderRows(widget.pageId, rowIds),
                      );
                }
              },
              builder: (context, candidateData, rejectedData) {
                print('DragTarget builder: candidateData=$candidateData');
                return DraggableRow(
                  key: ValueKey(entry.value.id),
                  row: entry.value,
                  onEdit: () => _showRowProperties(context, entry.value),
                  onDelete: () => _confirmDeleteRow(context, entry.value),
                  onReorder: (oldIndex, newIndex) {
                    final rows = state.rows;
                    if (oldIndex < newIndex) {
                      newIndex -= 1;
                    }
                    final item = rows.removeAt(oldIndex);
                    rows.insert(newIndex, item);

                    final rowIds = rows.map((r) => r.id).toList();
                    context.read<DynamicPageLayoutBloc>().add(
                          ReorderRows(widget.pageId, rowIds),
                        );
                  },
                  onAcceptCard: (cardData) {
                    final availableWidth = 12 - entry.value.cards.length;
                    context.read<DynamicPageLayoutBloc>().add(
                          AddCardToRow(entry.value.id, cardData,
                              gridWidth: availableWidth),
                        );
                  },
                  onReorderCards: (rowId, oldIndex, newIndex) {
                    context.read<DynamicPageLayoutBloc>().add(
                          ReorderCardsInRow(rowId, oldIndex, newIndex),
                        );
                  },
                );
              },
            );
          }).toList(),
          // Zone de drop pour nouvelle row
          DragTarget<String>(
            onWillAccept: (data) => data == 'row',
            onAccept: (data) {
              if (data == 'row') {
                context
                    .read<DynamicPageLayoutBloc>()
                    .add(AddRow(widget.pageId));
              }
            },
            builder: (context, candidateData, rejectedData) {
              return Container(
                height: 80,
                margin: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  border: Border.all(
                    color: candidateData.isNotEmpty
                        ? Theme.of(context).primaryColor
                        : Colors.grey.shade300,
                    width: candidateData.isNotEmpty ? 2 : 1,
                  ),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Center(
                  child: Text(
                    l10n.dropRowHere,
                    style: TextStyle(
                      color: Theme.of(context).primaryColor,
                    ),
                  ),
                ),
              );
            },
          ),
        ],
      );
    }
    return Center(child: Text(l10n.dropRowHere));
  }

  Future<void> _showRowProperties(BuildContext context, DynamicRow row) async {
    await showDialog(
      context: context,
      builder: (context) => RowPropertiesDialog(
        row: row,
        onSave: (alignment, crossAlignment, spacing) {
          context.read<DynamicPageLayoutBloc>().add(
                UpdateRowProperties(
                  row.id,
                  alignment,
                  crossAlignment,
                  spacing,
                ),
              );
        },
      ),
    );
  }

  Future<void> _confirmDeleteRow(BuildContext context, DynamicRow row) async {
    final l10n = AppLocalizations.of(context)!;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.deleteRow),
        content: Text(l10n.deleteRowConfirmation),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text(l10n.cancel),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: Text(l10n.delete),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      context.read<DynamicPageLayoutBloc>().add(DeleteRow(row.id));
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return BlocProvider(
      create: (context) => DynamicPageLayoutBloc(context.read<ApiClient>())
        ..add(LoadPageLayout(widget.pageId)),
      child: BlocBuilder<DynamicPageLayoutBloc, DynamicPageLayoutState>(
        builder: (context, state) {
          if (state is DynamicPageLayoutLoaded) {
            return Scaffold(
              appBar: AppBar(
                title: Text(l10n.pageLayout),
                actions: [
                  if (state is DynamicPageLayoutLoaded && state.isDirty)
                    IconButton(
                      icon: const Icon(Icons.refresh),
                      onPressed: () => context
                          .read<DynamicPageLayoutBloc>()
                          .add(ReloadPageLayout(widget.pageId)),
                    ),
                  IconButton(
                    icon: const Icon(Icons.save),
                    onPressed: state is DynamicPageLayoutSaving
                        ? null
                        : () => context.read<DynamicPageLayoutBloc>().add(
                              SaveLayout(widget.pageId),
                            ),
                  ),
                ],
              ),
              body: Row(
                children: [
                  // Menu latéral repliable
                  MouseRegion(
                    onEnter: (_) {
                      setState(() {
                        _isExpanded = true;
                      });
                    },
                    onExit: (_) {
                      _startCollapseTimer();
                    },
                    child: AnimatedContainer(
                      duration: const Duration(milliseconds: 200),
                      width: _isExpanded ? 250 : 70,
                      child: Card(
                        margin: const EdgeInsets.all(8.0),
                        child: Column(
                          children: [
                            // Bouton Pin
                            IconButton(
                              icon: Icon(
                                _isPinned
                                    ? Icons.push_pin
                                    : Icons.push_pin_outlined,
                                color: _isPinned ? Colors.blue : null,
                              ),
                              onPressed: () {
                                setState(() {
                                  _isPinned = !_isPinned;
                                  if (_isPinned) {
                                    _isExpanded = true;
                                    _collapseTimer?.cancel();
                                  } else {
                                    _startCollapseTimer();
                                  }
                                });
                              },
                            ),
                            Expanded(
                              child: ListView(
                                padding:
                                    const EdgeInsets.symmetric(vertical: 8.0),
                                children: [
                                  // Section Composants
                                  if (_isExpanded)
                                    Padding(
                                      padding: const EdgeInsets.symmetric(
                                          horizontal: 16.0),
                                      child: Text(l10n.components),
                                    )
                                  else
                                    const Divider(height: 1),
                                  const SizedBox(height: 8),
                                  // Row draggable
                                  _buildDraggableComponent(
                                    'row',
                                    Icons.table_rows,
                                    l10n.newRow,
                                  ),
                                  const SizedBox(height: 8),
                                  // PlaceholderCard draggable
                                  _buildDraggableComponent(
                                    'placeholder',
                                    Icons.widgets,
                                    l10n.placeholderCard,
                                  ),
                                  const SizedBox(height: 8),
                                  // TableCard draggable
                                  _buildDraggableComponent(
                                    'TableEntity',
                                    Icons.table_chart,
                                    l10n.tableCard,
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                  // Zone de contenu principal
                  Expanded(
                    child: Container(
                      margin: const EdgeInsets.all(8.0),
                      decoration: BoxDecoration(
                        border: Border.all(
                          color: Colors.grey.shade300,
                          width: 1,
                        ),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: ListView(
                        children: [
                          ...state.rows.map((row) => DraggableRow(
                                key: ValueKey(row.id),
                                row: row,
                                onEdit: () => _showRowProperties(context, row),
                                onDelete: () => _confirmDeleteRow(context, row),
                                onReorder: (oldIndex, newIndex) {
                                  final rows = state.rows;
                                  if (oldIndex < newIndex) {
                                    newIndex -= 1;
                                  }
                                  final item = rows.removeAt(oldIndex);
                                  rows.insert(newIndex, item);

                                  final rowIds = rows.map((r) => r.id).toList();
                                  context.read<DynamicPageLayoutBloc>().add(
                                        ReorderRows(widget.pageId, rowIds),
                                      );
                                },
                                onAcceptCard: (cardData) {
                                  final availableWidth = 12 - row.cards.length;
                                  context.read<DynamicPageLayoutBloc>().add(
                                        AddCardToRow(row.id, cardData,
                                            gridWidth: availableWidth),
                                      );
                                },
                                onReorderCards: (rowId, oldIndex, newIndex) {
                                  context.read<DynamicPageLayoutBloc>().add(
                                        ReorderCardsInRow(
                                            rowId, oldIndex, newIndex),
                                      );
                                },
                              )),
                          // Zone de drop pour nouvelle row
                          DragTarget<String>(
                            onWillAccept: (data) => data == 'row',
                            onAccept: (data) {
                              context
                                  .read<DynamicPageLayoutBloc>()
                                  .add(AddRow(widget.pageId));
                            },
                            builder: (context, candidateData, rejectedData) {
                              return Container(
                                height: 80,
                                margin: const EdgeInsets.all(8),
                                decoration: BoxDecoration(
                                  border: Border.all(
                                    color: candidateData.isNotEmpty
                                        ? Theme.of(context).primaryColor
                                        : Colors.grey.shade300,
                                    width: candidateData.isNotEmpty ? 2 : 1,
                                  ),
                                  borderRadius: BorderRadius.circular(8),
                                ),
                                child: Center(
                                  child: Text(
                                    l10n.dropRowHere,
                                    style: TextStyle(
                                      color: Theme.of(context).primaryColor,
                                    ),
                                  ),
                                ),
                              );
                            },
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            );
          }
          return const Center(child: CircularProgressIndicator());
        },
      ),
    );
  }

  Widget _buildDraggableComponent(String type, IconData icon, String label) {
    print('Building draggable component: type=$type');
    return Draggable<String>(
      data: type,
      feedback: Material(
        elevation: 4,
        child: Container(
          padding: const EdgeInsets.all(16.0),
          decoration: BoxDecoration(
            color: Theme.of(context).cardColor,
            borderRadius: BorderRadius.circular(8),
          ),
          child: Text(label),
        ),
      ),
      childWhenDragging: Container(),
      child: Container(
        height: 48,
        margin: const EdgeInsets.symmetric(horizontal: 4.0),
        padding: const EdgeInsets.symmetric(horizontal: 8.0),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(4),
          color: Theme.of(context).hoverColor,
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          mainAxisAlignment:
              _isExpanded ? MainAxisAlignment.start : MainAxisAlignment.center,
          children: [
            Icon(icon, size: 20),
            if (_isExpanded) ...[
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  label,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
