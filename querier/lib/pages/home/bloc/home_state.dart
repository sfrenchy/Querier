part of 'home_bloc.dart';

abstract class HomeState extends Equatable {
  const HomeState();

  @override
  List<Object> get props => [];
}

class HomeInitial extends HomeState {}

class HomeLoading extends HomeState {}

class HomeLoaded extends HomeState {
  final String username;
  final List<String> recentQueries;
  final Map<String, int> queryStats;
  final List<Map<String, dynamic>> activityData;

  const HomeLoaded({
    required this.username,
    required this.recentQueries,
    required this.queryStats,
    required this.activityData,
  });

  @override
  List<Object> get props => [username, recentQueries, queryStats, activityData];
}

class HomeError extends HomeState {
  final String message;

  const HomeError(this.message);

  @override
  List<Object> get props => [message];
}
