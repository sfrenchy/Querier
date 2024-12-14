import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/models/menu_category.dart';
import 'package:querier/pages/settings/menu/pages/bloc/dynamic_pages_bloc.dart';
import 'package:querier/pages/settings/menu/pages/bloc/dynamic_pages_event.dart';
import 'bloc/dynamic_menu_categories_bloc.dart';
import 'pages/dynamic_pages_screen.dart';

class DynamicMenuCategoriesScreen extends StatefulWidget {
  const DynamicMenuCategoriesScreen({super.key});

  @override
  State<DynamicMenuCategoriesScreen> createState() =>
      _DynamicMenuCategoriesScreenState();
}

class _DynamicMenuCategoriesScreenState
    extends State<DynamicMenuCategoriesScreen> {
  @override
  void initState() {
    super.initState();
    context.read<DynamicMenuCategoriesBloc>().add(LoadDynamicMenuCategories());
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final locale = Localizations.localeOf(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.menuCategories),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: l10n.addMenuCategory,
            onPressed: () async {
              final result = await Navigator.pushNamed(context, '/menu/form');
              if (result == true && mounted) {
                context
                    .read<DynamicMenuCategoriesBloc>()
                    .add(LoadDynamicMenuCategories());
              }
            },
          ),
        ],
      ),
      body: BlocBuilder<DynamicMenuCategoriesBloc, DynamicMenuCategoriesState>(
        builder: (context, state) {
          if (state is DynamicMenuCategoriesLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (state is DynamicMenuCategoriesError) {
            return Center(child: Text(state.message));
          }

          if (state is DynamicMenuCategoriesLoaded) {
            return Padding(
              padding: const EdgeInsets.all(16.0),
              child: Card(
                child: SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: ConstrainedBox(
                    constraints: BoxConstraints(
                      minWidth: MediaQuery.of(context).size.width - 32,
                    ),
                    child: DataTable(
                      columnSpacing: 24.0,
                      horizontalMargin: 24.0,
                      columns: [
                        DataColumn(
                          label: Expanded(
                            child: Text(l10n.name,
                                style: const TextStyle(
                                    fontWeight: FontWeight.bold)),
                          ),
                        ),
                        DataColumn(
                          label: Expanded(
                            child: Text(l10n.icon,
                                style: const TextStyle(
                                    fontWeight: FontWeight.bold)),
                          ),
                        ),
                        DataColumn(
                          label: Expanded(
                            child: Text(l10n.order,
                                style: const TextStyle(
                                    fontWeight: FontWeight.bold)),
                          ),
                        ),
                        DataColumn(
                          label: Expanded(
                            child: Text(l10n.visibility,
                                style: const TextStyle(
                                    fontWeight: FontWeight.bold)),
                          ),
                        ),
                        DataColumn(
                          label: Expanded(
                            child: Text(l10n.roles,
                                style: const TextStyle(
                                    fontWeight: FontWeight.bold)),
                          ),
                        ),
                        DataColumn(
                          label: Expanded(
                            child: Text(l10n.actions,
                                style: const TextStyle(
                                    fontWeight: FontWeight.bold)),
                          ),
                        ),
                      ],
                      rows: state.categories.map((category) {
                        return DataRow(
                          cells: [
                            DataCell(Text(category
                                .getLocalizedName(locale.languageCode))),
                            DataCell(Icon(category.getIconData())),
                            DataCell(Text(category.order.toString())),
                            DataCell(
                              Switch(
                                value: category.isVisible,
                                onChanged: (value) {
                                  context.read<DynamicMenuCategoriesBloc>().add(
                                        UpdateDynamicMenuCategoryVisibility(
                                            category, value),
                                      );
                                },
                              ),
                            ),
                            DataCell(Text(category.roles.join(', '))),
                            DataCell(
                              Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  IconButton(
                                    icon: const Icon(Icons.menu_book),
                                    tooltip: l10n.pages,
                                    onPressed: () {
                                      _showPages(context, category);
                                    },
                                  ),
                                  IconButton(
                                    icon: const Icon(Icons.edit),
                                    tooltip: l10n.edit,
                                    onPressed: () async {
                                      final result = await Navigator.pushNamed(
                                        context,
                                        '/menu/form',
                                        arguments: category,
                                      );
                                      if (result == true && mounted) {
                                        context
                                            .read<DynamicMenuCategoriesBloc>()
                                            .add(LoadDynamicMenuCategories());
                                      }
                                    },
                                  ),
                                  IconButton(
                                    icon: const Icon(Icons.delete),
                                    tooltip: l10n.delete,
                                    onPressed: () =>
                                        _showDeleteDialog(context, category),
                                  ),
                                ],
                              ),
                            ),
                          ],
                          onSelectChanged: (_) async {
                            final result = await Navigator.pushNamed(
                              context,
                              '/menu/form',
                              arguments: category,
                            );
                            if (result == true && mounted) {
                              context
                                  .read<DynamicMenuCategoriesBloc>()
                                  .add(LoadDynamicMenuCategories());
                            }
                          },
                        );
                      }).toList(),
                    ),
                  ),
                ),
              ),
            );
          }

          return const SizedBox();
        },
      ),
    );
  }

  void _showDeleteDialog(BuildContext context, MenuCategory category) {
    final l10n = AppLocalizations.of(context)!;
    final locale = Localizations.localeOf(context);

    showDialog(
      context: context,
      builder: (BuildContext context) {
        return AlertDialog(
          title: Text(l10n.delete),
          content: Text(
            '${l10n.deleteMenuCategory}: ${category.getLocalizedName(locale.languageCode)}?',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(context).pop(),
              child: Text(l10n.cancel),
            ),
            TextButton(
              onPressed: () {
                context
                    .read<DynamicMenuCategoriesBloc>()
                    .add(DeleteDynamicMenuCategory(category.Id));
                Navigator.of(context).pop();
              },
              child: Text(l10n.delete),
            ),
          ],
        );
      },
    );
  }

  void _showPages(BuildContext context, MenuCategory category) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => BlocProvider(
          create: (context) => DynamicPagesBloc(
            context.read<ApiClient>(),
          )..add(LoadPages(category.Id)),
          child: DynamicPagesScreen(category: category),
        ),
      ),
    );
  }
}
