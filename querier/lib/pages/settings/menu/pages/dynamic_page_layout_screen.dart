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
        children: state.rows.map((row) {
          return DraggableRow(
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
          );
        }).toList(),
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
      child: BlocConsumer<DynamicPageLayoutBloc, DynamicPageLayoutState>(
        listener: (context, state) {
          if (state is DynamicPageLayoutError) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text(state.message)),
            );
          }
        },
        builder: (context, state) {
          return Scaffold(
            appBar: AppBar(
              title: Text(l10n.pageLayout),
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
                                Draggable<String>(
                                  data: 'row',
                                  feedback: Material(
                                    child: Container(
                                      padding: const EdgeInsets.all(16.0),
                                      child: Text(l10n.newRow),
                                    ),
                                  ),
                                  childWhenDragging: Container(),
                                  child: Container(
                                    height: 48,
                                    margin: const EdgeInsets.symmetric(
                                        horizontal: 4.0),
                                    padding: EdgeInsets.symmetric(
                                      horizontal: _isExpanded ? 8.0 : 8.0,
                                    ),
                                    decoration: BoxDecoration(
                                      borderRadius: BorderRadius.circular(4),
                                      color: Theme.of(context).hoverColor,
                                    ),
                                    child: Row(
                                      mainAxisAlignment: _isExpanded
                                          ? MainAxisAlignment.start
                                          : MainAxisAlignment.center,
                                      children: [
                                        const Icon(Icons.table_rows),
                                        if (_isExpanded) ...[
                                          const SizedBox(width: 8),
                                          Flexible(
                                            child: Text(
                                              l10n.newRow,
                                              overflow: TextOverflow.ellipsis,
                                            ),
                                          ),
                                        ],
                                      ],
                                    ),
                                  ),
                                ),
                                const SizedBox(height: 8),
                                // PlaceholderCard draggable
                                Draggable<String>(
                                  data: 'placeholder',
                                  feedback: Material(
                                    child: Container(
                                      padding: const EdgeInsets.all(16.0),
                                      child: const Text('Placeholder Card'),
                                    ),
                                  ),
                                  childWhenDragging: Container(),
                                  child: Container(
                                    height: 48,
                                    margin: const EdgeInsets.symmetric(
                                        horizontal: 4.0),
                                    padding: EdgeInsets.symmetric(
                                      horizontal: _isExpanded ? 8.0 : 8.0,
                                    ),
                                    decoration: BoxDecoration(
                                      borderRadius: BorderRadius.circular(4),
                                      color: Theme.of(context).hoverColor,
                                    ),
                                    child: Row(
                                      mainAxisAlignment: _isExpanded
                                          ? MainAxisAlignment.start
                                          : MainAxisAlignment.center,
                                      children: [
                                        const Icon(Icons.widgets),
                                        if (_isExpanded) ...[
                                          const SizedBox(width: 8),
                                          Flexible(
                                            child: Text(
                                              'Placeholder Card',
                                              overflow: TextOverflow.ellipsis,
                                            ),
                                          ),
                                        ],
                                      ],
                                    ),
                                  ),
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
                    child: _buildMainContent(context, state),
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
