import 'package:flutter/material.dart';

abstract class PageLayoutEvent {}

class LoadPageLayout extends PageLayoutEvent {}

class SaveLayout extends PageLayoutEvent {}

class AddRow extends PageLayoutEvent {
  final MainAxisAlignment? alignment;
  final CrossAxisAlignment? crossAlignment;
  final double? spacing;

  AddRow({
    this.alignment,
    this.crossAlignment,
    this.spacing,
  });
}

class UpdateRow extends PageLayoutEvent {
  final int rowId;
  final MainAxisAlignment? alignment;
  final CrossAxisAlignment? crossAlignment;
  final double? spacing;

  UpdateRow(
    this.rowId, {
    this.alignment,
    this.crossAlignment,
    this.spacing,
  });
}

class DeleteRow extends PageLayoutEvent {
  final int rowId;

  DeleteRow(this.rowId);
}

class ReorderRows extends PageLayoutEvent {
  final List<int> rowIds;

  ReorderRows(this.rowIds);
}

class AddCard extends PageLayoutEvent {
  final int rowId;
  final String cardType;

  AddCard(this.rowId, this.cardType);
}

class DeleteCard extends PageLayoutEvent {
  final int cardId;

  DeleteCard(this.cardId);
}

class ReorderCards extends PageLayoutEvent {
  final int rowId;
  final List<int> cardIds;

  ReorderCards(this.rowId, this.cardIds);
}

class UpdateCardConfiguration extends PageLayoutEvent {
  final int cardId;
  final Map<String, String> titles;
  final bool isResizable;
  final bool isCollapsible;
  final double? height;
  final double? width;
  final String type;
  final int order;
  final bool useAvailableWidth;
  final bool useAvailableHeight;
  final Map<String, dynamic> configuration;

  UpdateCardConfiguration({
    required this.cardId,
    required this.titles,
    required this.isResizable,
    required this.isCollapsible,
    required this.height,
    required this.width,
    required this.type,
    required this.order,
    required this.useAvailableWidth,
    required this.useAvailableHeight,
    required this.configuration,
  });
}
