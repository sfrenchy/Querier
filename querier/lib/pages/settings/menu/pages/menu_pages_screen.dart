import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/blocs/menu_bloc.dart';
import 'package:querier/models/menu_category.dart';
import 'package:querier/models/page.dart';
import 'package:querier/pages/settings/menu/pages/bloc/menu_pages_bloc.dart';
import 'package:querier/pages/settings/menu/pages/bloc/menu_pages_event.dart';
import 'package:querier/pages/settings/menu/pages/bloc/menu_pages_state.dart';
import 'package:querier/pages/settings/menu/pages/menu_page_form.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_bloc.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_event.dart';
import 'package:querier/pages/settings/page_layout/page_layout_screen.dart';

class MenuPagesScreen extends StatelessWidget {
  final MenuCategory category;

  const MenuPagesScreen({super.key, required this.category});

  Future<void> _confirmDelete(BuildContext context, MenuPage page) async {
    final l10n = AppLocalizations.of(context)!;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.deletePageConfirmation),
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

    if (confirmed == true && context.mounted) {
      context.read<MenuPagesBloc>().add(DeletePage(page.id));
      // Rafraîchir le menu après la suppression
      context.read<MenuBloc>().add(LoadMenu());
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final locale = Localizations.localeOf(context);
    final apiClient = context.read<ApiClient>();

    return BlocProvider(
      create: (context) =>
          MenuPagesBloc(apiClient)..add(LoadPages(category.Id)),
      child: Builder(
        builder: (context) {
          final listBloc = context.read<MenuPagesBloc>();

          return Scaffold(
            appBar: AppBar(
              title: Text(
                  '${l10n.pages}: ${category.getLocalizedName(locale.languageCode)}'),
              actions: [
                IconButton(
                  icon: const Icon(Icons.add),
                  tooltip: l10n.add,
                  onPressed: () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (context) => MenuPageForm(
                          menuCategoryId: category.Id,
                          apiClient: apiClient,
                          onSaved: () {
                            listBloc.add(LoadPages(category.Id));
                          },
                        ),
                      ),
                    );
                  },
                ),
              ],
            ),
            body: BlocBuilder<MenuPagesBloc, MenuPagesState>(
              builder: (context, state) {
                if (state is MenuPagesLoading) {
                  return const Center(child: CircularProgressIndicator());
                }
                if (state is MenuPagesError) {
                  return Center(child: Text(state.message));
                }
                if (state is MenuPagesLoaded) {
                  return ListView.builder(
                    itemCount: state.pages.length,
                    itemBuilder: (context, index) {
                      final page = state.pages[index];
                      return ListTile(
                        leading: Icon(page.getIconData()),
                        title: Text(page.getLocalizedName(locale.languageCode)),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            IconButton(
                              icon: const Icon(Icons.grid_view),
                              tooltip: l10n.pageLayout,
                              onPressed: () {
                                Navigator.push(
                                  context,
                                  MaterialPageRoute(
                                    builder: (context) => BlocProvider(
                                      create: (context) => PageLayoutBloc(
                                        context.read<ApiClient>(),
                                        page.id,
                                      )..add(LoadPageLayout()),
                                      child: PageLayoutScreen(pageId: page.id),
                                    ),
                                  ),
                                );
                              },
                            ),
                            IconButton(
                              icon: const Icon(Icons.edit),
                              onPressed: () {
                                Navigator.push(
                                  context,
                                  MaterialPageRoute(
                                    builder: (context) => MenuPageForm(
                                      page: page,
                                      menuCategoryId: category.Id,
                                      apiClient: apiClient,
                                      onSaved: () {
                                        listBloc.add(LoadPages(category.Id));
                                      },
                                    ),
                                  ),
                                );
                              },
                            ),
                            IconButton(
                              icon: const Icon(Icons.delete),
                              onPressed: () {
                                _confirmDelete(context, page);
                              },
                            ),
                          ],
                        ),
                      );
                    },
                  );
                }
                return const SizedBox();
              },
            ),
          );
        },
      ),
    );
  }
}
