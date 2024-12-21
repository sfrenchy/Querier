import 'package:querier/models/sql_query.dart';

class SQLQueryRequest {
  final SQLQuery query;
  final Map<String, dynamic>? sampleParameters;

  SQLQueryRequest({
    required this.query,
    this.sampleParameters,
  });

  Map<String, dynamic> toJson() => {
        'query': query.toJson(),
        'sampleParameters': sampleParameters,
      };
}
