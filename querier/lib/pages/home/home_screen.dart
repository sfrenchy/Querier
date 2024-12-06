import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/api/api_client.dart';
import 'bloc/home_bloc.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

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
                          'assets/images/querier_logo_no_bg_big.png'),
                    ),
                    onPressed: () => Scaffold.of(context).openDrawer(),
                  ),
                ),
                title: Text(l10n.welcome(state.username)),
                actions: [
                  IconButton(
                    icon: const Icon(Icons.logout),
                    tooltip: l10n.logout,
                    onPressed: () {
                      context.read<HomeBloc>().add(LogoutRequested());
                      Navigator.pushReplacementNamed(context, '/login');
                    },
                  ),
                ],
              ),
              drawer: Drawer(
                child: ListView(
                  padding: EdgeInsets.zero,
                  children: [
                    DrawerHeader(
                      decoration: BoxDecoration(
                        color: Theme.of(context).primaryColor,
                      ),
                      child: Text(
                        l10n.settings,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 24,
                        ),
                      ),
                    ),
                    ExpansionTile(
                      leading: const Icon(Icons.settings),
                      title: Text(l10n.settings),
                      children: [
                        ListTile(
                          leading: const Icon(Icons.people),
                          title: Text(l10n.users),
                          onTap: () {
                            Navigator.pop(context);
                            Navigator.pushNamed(context, '/users');
                          },
                        ),
                        ListTile(
                          leading: const Icon(Icons.security),
                          title: Text(l10n.roles),
                          onTap: () {
                            Navigator.pop(context);
                            Navigator.pushNamed(context, '/roles');
                          },
                        ),
                        ListTile(
                          leading: const Icon(Icons.miscellaneous_services),
                          title: Text(l10n.services),
                          onTap: () {
                            Navigator.pop(context);
                            Navigator.pushNamed(context, '/services');
                          },
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              body: RefreshIndicator(
                onRefresh: () async {
                  context.read<HomeBloc>().add(RefreshDashboard());
                },
                child: SingleChildScrollView(
                  child: Padding(
                    padding: const EdgeInsets.all(16.0),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        _buildStatsCards(state),
                        const SizedBox(height: 24),
                        _buildActivityChart(state),
                        const SizedBox(height: 24),
                        _buildRecentQueries(state),
                      ],
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
