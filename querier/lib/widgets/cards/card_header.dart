import 'package:flutter/material.dart';

class CardHeader extends StatelessWidget {
  final String title;
  final VoidCallback? onEdit;
  final VoidCallback? onDelete;
  final Widget? dragHandle;

  const CardHeader({
    super.key,
    required this.title,
    this.onEdit,
    this.onDelete,
    this.dragHandle,
  });

  @override
  Widget build(BuildContext context) {
    return ListTile(
      title: Text(title),
      trailing: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (onEdit != null)
            IconButton(
              icon: const Icon(Icons.edit),
              onPressed: onEdit,
            ),
          if (dragHandle != null)
            dragHandle!,
          if (onDelete != null)
            IconButton(
              icon: const Icon(Icons.delete),
              onPressed: onDelete,
            ),
        ],
      ),
    );
  }
}
