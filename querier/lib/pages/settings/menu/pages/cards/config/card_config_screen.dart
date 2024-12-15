import 'package:flutter/material.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/widgets/translation_manager.dart';
import 'package:querier/widgets/color_picker_button.dart';

class CardConfigScreen extends StatefulWidget {
  final DynamicCard card;
  final ValueChanged<DynamicCard> onSave;

  const CardConfigScreen({
    Key? key,
    required this.card,
    required this.onSave,
  }) : super(key: key);

  @override
  State<CardConfigScreen> createState() => _CardConfigScreenState();
}

class _CardConfigScreenState extends State<CardConfigScreen> {
  late Map<String, String> titles;
  Color? backgroundColor;
  Color? textColor;
  bool useAvailableWidth = true;
  bool useAvailableHeight = true;
  double? width;
  double? height;
  
  // Ajout des états d'expansion
  final List<bool> _isExpanded = [false, false, false];  // Title, Colors, Dimensions

  @override
  void initState() {
    super.initState();
    titles = Map.from(widget.card.titles);
    backgroundColor = widget.card.backgroundColor != null 
      ? Color(widget.card.backgroundColor!) 
      : null;
    textColor = widget.card.textColor != null 
      ? Color(widget.card.textColor!) 
      : null;
    useAvailableWidth = widget.card.useAvailableWidth;
    useAvailableHeight = widget.card.useAvailableHeight;
    width = widget.card.width;
    height = widget.card.height;
  }

  void _save() {
    final updatedCard = widget.card.copyWith(
      titles: titles,
      backgroundColor: backgroundColor?.value,
      textColor: textColor?.value,
      useAvailableWidth: useAvailableWidth,
      useAvailableHeight: useAvailableHeight,
      width: width,
      height: height,
    );
    widget.onSave(updatedCard);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.configureCard),
        actions: [
          IconButton(
            icon: const Icon(Icons.save),
            onPressed: _save,
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: ExpansionPanelList(
          expandedHeaderPadding: EdgeInsets.zero,
          expansionCallback: (index, isExpanded) {
            setState(() {
              _isExpanded[index] = isExpanded;
            });
          },
          children: [
            // Titre
            ExpansionPanel(
              canTapOnHeader: true,
              headerBuilder: (context, isExpanded) => 
                Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Text(l10n.cardTitle),
                ),
              body: Padding(
                padding: const EdgeInsets.all(16.0),
                child: TranslationManager(
                  translations: titles,
                  onTranslationsChanged: (newTitles) {
                    setState(() => titles = newTitles);
                  },
                ),
              ),
              isExpanded: _isExpanded[0],
            ),
            // Couleurs
            ExpansionPanel(
              canTapOnHeader: true,
              headerBuilder: (context, isExpanded) => 
                Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Text(l10n.colors),
                ),
              body: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  children: [
                    ListTile(
                      title: Text(l10n.backgroundColor),
                      trailing: ColorPickerButton(
                        color: backgroundColor,
                        onColorChanged: (color) {
                          setState(() => backgroundColor = color);
                        },
                      ),
                    ),
                    ListTile(
                      title: Text(l10n.textColor),
                      trailing: ColorPickerButton(
                        color: textColor,
                        onColorChanged: (color) {
                          setState(() => textColor = color);
                        },
                      ),
                    ),
                  ],
                ),
              ),
              isExpanded: _isExpanded[1],
            ),
            // Dimensions
            ExpansionPanel(
              canTapOnHeader: true,
              headerBuilder: (context, isExpanded) => 
                Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Text(l10n.dimensions),
                ),
              body: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  children: [
                    SwitchListTile(
                      title: Text(l10n.useAvailableWidth),
                      value: useAvailableWidth,
                      onChanged: (value) {
                        setState(() => useAvailableWidth = value);
                      },
                    ),
                    if (!useAvailableWidth)
                      TextFormField(
                        decoration: InputDecoration(
                          labelText: l10n.cardWidth,
                        ),
                        initialValue: width?.toString(),
                        keyboardType: TextInputType.number,
                        onChanged: (value) {
                          setState(() {
                            width = double.tryParse(value);
                          });
                        },
                      ),
                    SwitchListTile(
                      title: Text(l10n.useAvailableHeight),
                      value: useAvailableHeight,
                      onChanged: (value) {
                        setState(() => useAvailableHeight = value);
                      },
                    ),
                    if (!useAvailableHeight)
                      TextFormField(
                        decoration: InputDecoration(
                          labelText: l10n.cardHeight,
                        ),
                        initialValue: height?.toString(),
                        keyboardType: TextInputType.number,
                        onChanged: (value) {
                          setState(() {
                            height = double.tryParse(value);
                          });
                        },
                      ),
                  ],
                ),
              ),
              isExpanded: _isExpanded[2],
            ),
          ],
        ),
      ),
    );
  }
} 