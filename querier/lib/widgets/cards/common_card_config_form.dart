import 'package:flutter/material.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/widgets/translation_manager.dart';
import 'package:flutter_colorpicker/flutter_colorpicker.dart';

class CommonCardConfigForm extends StatefulWidget {
  final Map<String, String> titles;
  final bool isResizable;
  final bool isCollapsible;
  final double? height;
  final double? width;
  final bool useAvailableWidth;
  final bool useAvailableHeight;
  final Function(Map<String, String>) onTitlesChanged;
  final Function(bool) onResizableChanged;
  final Function(bool) onCollapsibleChanged;
  final Function(double?) onHeightChanged;
  final Function(double?) onWidthChanged;
  final Function(bool) onUseAvailableWidthChanged;
  final Function(bool) onUseAvailableHeightChanged;
  final Color backgroundColor;
  final Color textColor;
  final Function(Color) onBackgroundColorChanged;
  final Function(Color) onTextColorChanged;

  const CommonCardConfigForm({
    super.key,
    required this.titles,
    required this.isResizable,
    required this.isCollapsible,
    required this.height,
    required this.width,
    required this.useAvailableWidth,
    required this.useAvailableHeight,
    required this.onTitlesChanged,
    required this.onResizableChanged,
    required this.onCollapsibleChanged,
    required this.onHeightChanged,
    required this.onWidthChanged,
    required this.onUseAvailableWidthChanged,
    required this.onUseAvailableHeightChanged,
    required this.backgroundColor,
    required this.textColor,
    required this.onBackgroundColorChanged,
    required this.onTextColorChanged,
  });

  @override
  State<CommonCardConfigForm> createState() => _CommonCardConfigFormState();
}

class _CommonCardConfigFormState extends State<CommonCardConfigForm> {
  late final TextEditingController _heightController;
  late final TextEditingController _widthController;
  late final Map<String, TextEditingController> _titleControllers;
  late final FocusNode _heightFocus;
  late final FocusNode _widthFocus;
  int _expandedIndex = -1;

  @override
  void initState() {
    super.initState();
    _heightController =
        TextEditingController(text: widget.height?.toString() ?? '');
    _widthController =
        TextEditingController(text: widget.width?.toString() ?? '');
    _heightFocus = FocusNode();
    _widthFocus = FocusNode();
    _titleControllers = widget.titles.map(
      (key, value) => MapEntry(key, TextEditingController(text: value)),
    );

    _titleControllers.forEach((key, controller) {
      controller.addListener(() => _onTitleChanged());
    });
  }

  @override
  void dispose() {
    _heightController.dispose();
    _widthController.dispose();
    _heightFocus.dispose();
    _widthFocus.dispose();
    _titleControllers.values.forEach((controller) {
      controller.dispose();
    });
    super.dispose();
  }

  void _onTitleChanged() {
    final titles = _titleControllers
        .map((key, controller) => MapEntry(key, controller.text));
    widget.onTitlesChanged(titles);
  }

  @override
  void didUpdateWidget(CommonCardConfigForm oldWidget) {
    super.didUpdateWidget(oldWidget);

    // Mettre à jour les contrôleurs de hauteur et largeur
    if (widget.height != oldWidget.height) {
      _heightController.text = widget.height?.toString() ?? '';
    }
    if (widget.width != oldWidget.width) {
      _widthController.text = widget.width?.toString() ?? '';
    }

    // Mettre à jour les contrôleurs de titre si nécessaire
    if (widget.titles != oldWidget.titles) {
      // Mettre à jour les valeurs des contrôleurs existants
      widget.titles.forEach((key, value) {
        if (_titleControllers.containsKey(key)) {
          if (_titleControllers[key]!.text != value) {
            _titleControllers[key]!.text = value;
          }
        } else {
          // Créer un nouveau contrôleur si la langue n'existe pas
          _titleControllers[key] = TextEditingController(text: value);
        }
      });

      // Supprimer les contrôleurs qui ne sont plus nécessaires
      _titleControllers
          .removeWhere((key, _) => !widget.titles.containsKey(key));
    }
  }

  Widget _buildColorsSection(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Colors',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: ListTile(
                    title: const Text('Background Color'),
                    subtitle: Container(
                      height: 24,
                      decoration: BoxDecoration(
                        color: widget.backgroundColor,
                        border: Border.all(color: Colors.grey),
                        borderRadius: BorderRadius.circular(4),
                      ),
                    ),
                    onTap: () => _showColorPicker(
                      context,
                      'Background Color',
                      widget.backgroundColor,
                      widget.onBackgroundColorChanged,
                    ),
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: ListTile(
                    title: const Text('Text Color'),
                    subtitle: Container(
                      height: 24,
                      decoration: BoxDecoration(
                        color: widget.textColor,
                        border: Border.all(color: Colors.grey),
                        borderRadius: BorderRadius.circular(4),
                      ),
                    ),
                    onTap: () => _showColorPicker(
                      context,
                      'Text Color',
                      widget.textColor,
                      widget.onTextColorChanged,
                    ),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  void _showColorPicker(
    BuildContext context,
    String title,
    Color initialColor,
    Function(Color) onColorChanged,
  ) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(title),
        content: SingleChildScrollView(
          child: ColorPicker(
            pickerColor: initialColor,
            onColorChanged: onColorChanged,
            pickerAreaHeightPercent: 0.8,
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('OK'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return Column(
      children: [
        ExpansionPanelList(
          expansionCallback: (index, isExpanded) {
            setState(() {
              _expandedIndex = _expandedIndex == index ? -1 : index;
            });
          },
          children: [
            ExpansionPanel(
              headerBuilder: (context, isExpanded) {
                return ListTile(
                  title: Text(l10n.cardTitle),
                );
              },
              body: Padding(
                padding: const EdgeInsets.all(16.0),
                child: TranslationManager(
                  translations: _titleControllers,
                  onTranslationsChanged: widget.onTitlesChanged,
                ),
              ),
              isExpanded: _expandedIndex == 0,
              canTapOnHeader: true,
            ),
            ExpansionPanel(
              headerBuilder: (context, isExpanded) {
                return ListTile(
                  title: Text(l10n.dimensions),
                );
              },
              body: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  children: [
                    SwitchListTile(
                      title: Text(l10n.cardIsResizable),
                      value: widget.isResizable,
                      onChanged: widget.onResizableChanged,
                    ),
                    SwitchListTile(
                      title: Text(l10n.cardIsCollapsible),
                      value: widget.isCollapsible,
                      onChanged: widget.onCollapsibleChanged,
                    ),
                    SwitchListTile(
                      title: Text(l10n.useAvailableWidth),
                      value: widget.useAvailableWidth,
                      onChanged: widget.onUseAvailableWidthChanged,
                    ),
                    SwitchListTile(
                      title: Text(l10n.useAvailableHeight),
                      value: widget.useAvailableHeight,
                      onChanged: widget.onUseAvailableHeightChanged,
                    ),
                    if (!widget.useAvailableWidth) ...[
                      Focus(
                        child: TextFormField(
                          controller: _widthController,
                          focusNode: _widthFocus,
                          decoration:
                              InputDecoration(labelText: l10n.cardWidth),
                          keyboardType: const TextInputType.numberWithOptions(
                              decimal: true),
                        ),
                        onFocusChange: (hasFocus) {
                          if (!hasFocus) {
                            final value = _widthController.text;
                            widget.onWidthChanged(
                              value.isEmpty ? null : double.tryParse(value),
                            );
                          }
                        },
                      ),
                    ],
                    if (!widget.useAvailableHeight) ...[
                      Focus(
                        child: TextFormField(
                          controller: _heightController,
                          focusNode: _heightFocus,
                          decoration:
                              InputDecoration(labelText: l10n.cardHeight),
                          keyboardType: const TextInputType.numberWithOptions(
                              decimal: true),
                        ),
                        onFocusChange: (hasFocus) {
                          if (!hasFocus) {
                            final value = _heightController.text;
                            widget.onHeightChanged(
                              value.isEmpty ? null : double.tryParse(value),
                            );
                          }
                        },
                      ),
                    ],
                  ],
                ),
              ),
              isExpanded: _expandedIndex == 1,
              canTapOnHeader: true,
            ),
            ExpansionPanel(
              headerBuilder: (context, isExpanded) {
                return ListTile(
                  title: Text(l10n.colors),
                );
              },
              body: _buildColorsSection(context),
              isExpanded: _expandedIndex == 2,
              canTapOnHeader: true,
            ),
          ],
        ),
      ],
    );
  }
}
