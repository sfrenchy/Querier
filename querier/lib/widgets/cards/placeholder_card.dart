import 'package:flutter/material.dart';

class PlaceholderCard extends StatelessWidget {
  final String title;
  final double? height;
  final double? width;
  final bool isResizable;
  final bool isCollapsible;

  const PlaceholderCard({
    super.key,
    required this.title,
    this.height,
    this.width,
    this.isResizable = false,
    this.isCollapsible = true,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Container(
        height: height,
        width: width,
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  title,
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                if (isCollapsible)
                  IconButton(
                    icon: const Icon(Icons.more_vert),
                    onPressed: () {
                      // Menu pour éditer/supprimer la carte
                    },
                  ),
              ],
            ),
            const Expanded(
              child: Center(
                child: Text('Placeholder Card'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
