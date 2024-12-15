import 'package:equatable/equatable.dart';
import 'package:querier/models/dynamic_row.dart';

abstract class DynamicPageLayoutState extends Equatable {
  const DynamicPageLayoutState();

  @override
  List<Object> get props => [];
}

class DynamicPageLayoutInitial extends DynamicPageLayoutState {}

class DynamicPageLayoutLoading extends DynamicPageLayoutState {}

class DynamicPageLayoutLoaded extends DynamicPageLayoutState {
  final List<DynamicRow> rows;

  const DynamicPageLayoutLoaded(this.rows);

  @override
  List<Object> get props => [rows];
}

class DynamicPageLayoutError extends DynamicPageLayoutState {
  final String message;

  const DynamicPageLayoutError(this.message);

  @override
  List<Object> get props => [message];
}

class DynamicPageLayoutSaving extends DynamicPageLayoutState {}
