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
