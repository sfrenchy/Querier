import 'package:equatable/equatable.dart';
import 'package:querier/models/dynamic_row.dart';

abstract class DynamicPageLayoutEvent extends Equatable {
  const DynamicPageLayoutEvent();

  @override
  List<Object> get props => [];
}

class LoadPageLayout extends DynamicPageLayoutEvent {
  final int pageId;

  const LoadPageLayout(this.pageId);

  @override
  List<Object> get props => [pageId];
}

class AddRow extends DynamicPageLayoutEvent {
  final int pageId;

  const AddRow(this.pageId);

  @override
  List<Object> get props => [pageId];
}

class ReorderRows extends DynamicPageLayoutEvent {
  final int pageId;
  final List<int> rowIds;

  const ReorderRows(this.pageId, this.rowIds);

  @override
  List<Object> get props => [pageId, rowIds];
}
