import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/blocs/menu_bloc.dart';
import 'package:querier/widgets/app_drawer.dart';
import 'package:querier/widgets/cards/card_selector.dart';
import 'package:querier/widgets/menu_drawer.dart';
import 'package:querier/widgets/user_avatar.dart';
import 'bloc/home_bloc.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    context.read<MenuBloc>().add(LoadMenu());

    return BlocProvider(
      create: (context) =>
          HomeBloc(context.read<ApiClient>())..add(LoadDashboard()),
      child: BlocBuilder<HomeBloc, HomeState>(
        builder: (context, state) {
          if (state is HomeLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (state is HomeError) {
            return Center(child: Text(state.message));
          }

          if (state is HomeLoaded) {
            return Scaffold(
              appBar: AppBar(
                leading: Builder(
                  builder: (BuildContext context) => IconButton(
                    icon: Padding(
                      padding: const EdgeInsets.all(8.0),
                      child: Image.asset(
                        'assets/images/querier_logo_no_bg_big.png',
                        width: 40,
                        height: 40,
                      ),
                    ),
                    onPressed: () => Scaffold.of(context).openDrawer(),
                  ),
                ),
                actions: [
                  Padding(
                    padding: const EdgeInsets.all(8.0),
                    child: UserAvatar(
                      firstName: state.firstName,
                      lastName: state.lastName,
                      onTap: () => Navigator.pushNamed(context, '/profile'),
                    ),
                  ),
                ],
              ),
              drawer: const AppDrawer(),
              body: RefreshIndicator(
                onRefresh: () async {
                  context.read<HomeBloc>().add(RefreshDashboard());
                },
                child: SingleChildScrollView(
                  padding: const EdgeInsets.all(8.0),
                  child: Container(
                    margin: const EdgeInsets.all(4.0),
                    decoration: BoxDecoration(
                      border: Border.all(
                        color: Colors.grey.shade300,
                        width: 1,
                      ),
                      borderRadius: BorderRadius.circular(4),
                    ),
                    child: Column(
                      children: state.rows
                          .map((row) => Container(
                                padding: const EdgeInsets.all(8.0),
                                child: Row(
                                  mainAxisAlignment:
                                      row.alignment ?? MainAxisAlignment.start,
                                  crossAxisAlignment: row.crossAlignment ??
                                      CrossAxisAlignment.start,
                                  children: row.cards.map((card) {
                                    // Calculer la largeur disponible en tenant compte de tous les paddings
                                    final totalPadding =
                                        24.0 + // Padding externe (8.0 de chaque côté)
                                            8.0 + // Margin du Container (4.0 de chaque côté)
                                            16.0 + // Padding des rows (8.0 de chaque côté)
                                            (row.spacing?.toDouble() ?? 8.0) *
                                                2; // Spacing entre cartes

                                    final availableWidth =
                                        MediaQuery.of(context).size.width -
                                            totalPadding;
                                    final width =
                                        (card.gridWidth / 12) * availableWidth;

                                    return SizedBox(
                                      width: width,
                                      child: Padding(
                                        padding: EdgeInsets.symmetric(
                                            horizontal:
                                                row.spacing?.toDouble() ?? 8.0),
                                        child: CardSelector(
                                          card: card,
                                          onEdit: () {},
                                          onDelete: () {},
                                        ),
                                      ),
                                    );
                                  }).toList(),
                                ),
                              ))
                          .toList(),
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

  Widget _buildStatsCards(HomeLoaded state) {
    return GridView.count(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      crossAxisCount: 2,
      crossAxisSpacing: 16,
      mainAxisSpacing: 16,
      children: state.queryStats.entries.map((entry) {
        return Card(
          child: Padding(
            padding: const EdgeInsets.all(16.0),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  entry.value.toString(),
                  style: const TextStyle(
                    fontSize: 24,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 8),
                Text(entry.key),
              ],
            ),
          ),
        );
      }).toList(),
    );
  }

  Widget _buildActivityChart(HomeLoaded state) {
    return SizedBox(
      height: 200,
      child: LineChart(
        LineChartData(
          gridData: const FlGridData(show: false),
          titlesData: const FlTitlesData(show: false),
          borderData: FlBorderData(show: false),
          lineBarsData: [
            LineChartBarData(
              spots: state.activityData
                  .asMap()
                  .entries
                  .map((entry) => FlSpot(
                        entry.key.toDouble(),
                        entry.value['value'].toDouble(),
                      ))
                  .toList(),
              isCurved: true,
              color: Colors.blue,
              barWidth: 3,
              dotData: const FlDotData(show: false),
              belowBarData: BarAreaData(show: true),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildRecentQueries(HomeLoaded state) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Recent Queries',
          style: const TextStyle(
            fontSize: 20,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: 16),
        if (state.recentQueries.isEmpty)
          Center(child: Text('No recent queries')),
        ListView.builder(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          itemCount: state.recentQueries.length,
          itemBuilder: (context, index) {
            return ListTile(
              leading: const Icon(Icons.history),
              title: Text(state.recentQueries[index]),
            );
          },
        ),
      ],
    );
  }
}
