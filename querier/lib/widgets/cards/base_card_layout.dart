import 'package:flutter/material.dart';

class BaseCardLayout extends StatelessWidget {
  final Widget child;
  final bool useAvailableWidth;
  final bool useAvailableHeight;
  final double? width;
  final double? height;

  const BaseCardLayout({
    super.key,
    required this.child,
    this.useAvailableWidth = true,
    this.useAvailableHeight = true,
    this.width,
    this.height,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: useAvailableWidth ? double.infinity : width,
      height: useAvailableHeight ? height ?? 300 : height,
      constraints: const BoxConstraints(
        minHeight: 100,
      ),
      child: child,
    );
  }
}
