import 'package:querier/models/dynamic_row.dart';

abstract class PageLayoutState {}

class PageLayoutLoading extends PageLayoutState {}

class PageLayoutError extends PageLayoutState {
  final String message;

  PageLayoutError(this.message);
}

class PageLayoutLoaded extends PageLayoutState {
  final List<DynamicRow> rows;
  final bool isDirty;

  PageLayoutLoaded(this.rows, {this.isDirty = false});

  PageLayoutLoaded copyWith({
    List<DynamicRow>? rows,
    bool? isDirty,
  }) {
    return PageLayoutLoaded(
      rows ?? this.rows,
      isDirty: isDirty ?? this.isDirty,
    );
  }
}
