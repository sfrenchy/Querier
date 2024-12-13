import 'package:flutter/material.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/widgets/translation_manager.dart';

class CommonCardConfigForm extends StatelessWidget {
  final Map<String, String> titles;
  final bool isResizable;
  final bool isCollapsible;
  final double? height;
  final double? width;
  final Function(Map<String, String>) onTitlesChanged;
  final Function(bool) onResizableChanged;
  final Function(bool) onCollapsibleChanged;
  final Function(double?) onHeightChanged;
  final Function(double?) onWidthChanged;
  final bool useAvailableWidth;
  final Function(bool) onUseAvailableWidthChanged;

  const CommonCardConfigForm({
    super.key,
    required this.titles,
    required this.isResizable,
    required this.isCollapsible,
    required this.height,
    required this.width,
    required this.onTitlesChanged,
    required this.onResizableChanged,
    required this.onCollapsibleChanged,
    required this.onHeightChanged,
    required this.onWidthChanged,
    required this.useAvailableWidth,
    required this.onUseAvailableWidthChanged,
  });

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    // Convertir Map<String, String> en Map<String, TextEditingController>
    final titleControllers = titles.map(
      (key, value) => MapEntry(key, TextEditingController(text: value)),
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Utilisation de TranslationManager pour les titres
        TranslationManager(
          translations: titleControllers,
          onTranslationsChanged: (controllers) {
            final newTitles = controllers.map(
              (key, controller) => MapEntry(key, controller.text),
            );
            onTitlesChanged(newTitles);
          },
        ),

        const SizedBox(height: 16),

        // Options de redimensionnement et réduction
        SwitchListTile(
          title: Text(l10n.cardIsResizable),
          value: isResizable,
          onChanged: onResizableChanged,
        ),

        SwitchListTile(
          title: Text(l10n.cardIsCollapsible),
          value: isCollapsible,
          onChanged: onCollapsibleChanged,
        ),

        // Dimensions
        const SizedBox(height: 16),
        Text(l10n.dimensions, style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: 8),

        // Option pour utiliser la largeur disponible
        SwitchListTile(
          title: Text(l10n.useAvailableWidth),
          value: useAvailableWidth,
          onChanged: onUseAvailableWidthChanged,
        ),

        // Champs de dimensions
        if (!useAvailableWidth) ...[
          const SizedBox(height: 8),
          Row(
            children: [
              Expanded(
                child: TextFormField(
                  initialValue: width?.toString() ?? '',
                  decoration: InputDecoration(
                    labelText: l10n.cardWidth,
                    border: const OutlineInputBorder(),
                    suffixText: 'px',
                  ),
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                  onChanged: (value) {
                    onWidthChanged(
                        value.isEmpty ? null : double.tryParse(value));
                  },
                ),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: TextFormField(
                  initialValue: height?.toString() ?? '',
                  decoration: InputDecoration(
                    labelText: l10n.cardHeight,
                    border: const OutlineInputBorder(),
                    suffixText: 'px',
                  ),
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                  onChanged: (value) {
                    onHeightChanged(
                        value.isEmpty ? null : double.tryParse(value));
                  },
                ),
              ),
            ],
          ),
        ],
      ],
    );
  }
}
