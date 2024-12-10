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
  final String firstName;
  final String lastName;
  final Map<String, int> queryStats;
  final List<Map<String, dynamic>> activityData;
  final List<String> recentQueries;

  const HomeLoaded({
    required this.username,
    required this.firstName,
    required this.lastName,
    required this.queryStats,
    required this.activityData,
    required this.recentQueries,
  });

  @override
  List<Object> get props =>
      [username, firstName, lastName, queryStats, activityData, recentQueries];
}

class HomeError extends HomeState {
  final String message;

  const HomeError(this.message);

  @override
  List<Object> get props => [message];
}
