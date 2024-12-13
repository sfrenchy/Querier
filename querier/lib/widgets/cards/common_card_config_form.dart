import 'package:flutter/material.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/widgets/translation_manager.dart';

class CommonCardConfigForm extends StatefulWidget {
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
  final bool useAvailableHeight;
  final Function(bool) onUseAvailableHeightChanged;

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
    required this.useAvailableHeight,
    required this.onUseAvailableHeightChanged,
  });

  @override
  State<CommonCardConfigForm> createState() => _CommonCardConfigFormState();
}

class _CommonCardConfigFormState extends State<CommonCardConfigForm> {
  int _expandedIndex = 0;
  late Map<String, TextEditingController> _titleControllers;

  @override
  void initState() {
    super.initState();
    _initTitleControllers();
  }

  void _initTitleControllers() {
    print('Initializing title controllers with: ${widget.titles}');
    _titleControllers = widget.titles.map(
      (key, value) => MapEntry(key, TextEditingController(text: value)),
    );
  }

  @override
  void didUpdateWidget(CommonCardConfigForm oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.titles != oldWidget.titles) {
      for (var entry in widget.titles.entries) {
        if (_titleControllers.containsKey(entry.key)) {
          _titleControllers[entry.key]!.text = entry.value;
        } else {
          _titleControllers[entry.key] =
              TextEditingController(text: entry.value);
        }
      }
      _titleControllers
          .removeWhere((key, _) => !widget.titles.containsKey(key));
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return ExpansionPanelList(
      expansionCallback: (index, isExpanded) {
        setState(() => _expandedIndex = isExpanded ? index : -1);
      },
      children: [
        ExpansionPanel(
          headerBuilder: (context, isExpanded) {
            return ListTile(title: Text(l10n.cardTitle));
          },
          body: Padding(
            padding: const EdgeInsets.all(16.0),
            child: TranslationManager(
              translations: _titleControllers,
              onTranslationsChanged: (newValues) {
                final updatedTitles =
                    newValues.map((key, value) => MapEntry(key, value));
                widget.onTitlesChanged(updatedTitles);
              },
            ),
          ),
          isExpanded: _expandedIndex == 0,
        ),
        ExpansionPanel(
          headerBuilder: (context, isExpanded) {
            return ListTile(title: Text(l10n.dimensions));
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
                  TextFormField(
                    decoration: InputDecoration(labelText: l10n.cardWidth),
                    initialValue: widget.width?.toString(),
                    keyboardType:
                        const TextInputType.numberWithOptions(decimal: true),
                    onChanged: (value) {
                      widget.onWidthChanged(
                          value.isEmpty ? null : double.tryParse(value));
                    },
                  ),
                ],
                if (!widget.useAvailableHeight) ...[
                  TextFormField(
                    decoration: InputDecoration(labelText: l10n.cardHeight),
                    initialValue: widget.height?.toString(),
                    keyboardType:
                        const TextInputType.numberWithOptions(decimal: true),
                    onChanged: (value) {
                      widget.onHeightChanged(
                          value.isEmpty ? null : double.tryParse(value));
                    },
                  ),
                ],
              ],
            ),
          ),
          isExpanded: _expandedIndex == 1,
        ),
      ],
    );
  }

  @override
  void dispose() {
    for (var controller in _titleControllers.values) {
      controller.dispose();
    }
    super.dispose();
  }
}
