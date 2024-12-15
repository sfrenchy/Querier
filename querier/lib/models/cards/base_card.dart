abstract class BaseCard {
  final int id;
  final Map<String, String> titles;
  final int order;
  final String type;
  final double? height;
  final double? width;
  final bool useAvailableWidth;
  final bool useAvailableHeight;
  final int? backgroundColor;
  final int? textColor;

  const BaseCard({
    required this.id,
    required this.titles,
    required this.order,
    required this.type,
    this.height,
    this.width,
    this.useAvailableWidth = true,
    this.useAvailableHeight = true,
    this.backgroundColor,
    this.textColor,
  });

  Map<String, dynamic> get specificConfiguration;

  Map<String, dynamic> toJson() => {
        'Id': id,
        'Titles': titles,
        'Order': order,
        'Type': type,
        'Height': height,
        'Width': width,
        'UseAvailableWidth': useAvailableWidth,
        'UseAvailableHeight': useAvailableHeight,
        'BackgroundColor': backgroundColor,
        'TextColor': textColor,
        'Configuration': specificConfiguration,
      };

  String getLocalizedTitle(String languageCode) {
    return titles[languageCode] ?? titles['en'] ?? '';
  }
}
