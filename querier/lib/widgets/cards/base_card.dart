import 'package:flutter/material.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_bloc.dart';

class BaseCard extends StatelessWidget {
  final String title;
  final int cardId;
  final bool isEditable;
  final PageLayoutBloc pageLayoutBloc;
  final Widget child;
  final VoidCallback onConfigurePressed;
  final double? height;
  final double? width;
  final bool useAvailableWidth;
  final bool useAvailableHeight;

  const BaseCard({
    super.key,
    required this.title,
    required this.cardId,
    required this.isEditable,
    required this.pageLayoutBloc,
    required this.child,
    required this.onConfigurePressed,
    this.height,
    this.width,
    this.useAvailableWidth = false,
    this.useAvailableHeight = false,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: useAvailableWidth ? double.infinity : width,
      height: height ?? 300,
      constraints: BoxConstraints(
        minWidth: width != null ? width! : 200.0,
        minHeight: 100,
        maxWidth: width ?? double.infinity,
      ),
      decoration: BoxDecoration(
        color: Theme.of(context).cardColor,
        borderRadius: BorderRadius.circular(8),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.1),
            blurRadius: 4,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.all(8.0),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    title,
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                  if (isEditable)
                    IconButton(
                      icon: const Icon(Icons.settings),
                      onPressed: onConfigurePressed,
                      tooltip: 'Configure',
                    ),
                ],
              ),
            ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16.0),
              child: Container(
                height: 1,
                color: Colors.white,
                margin: const EdgeInsets.only(bottom: 8.0),
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(8.0),
              child: child,
            ),
          ],
        ),
      ),
    );
  }
}
