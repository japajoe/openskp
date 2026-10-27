import 'dart:typed_data';

import 'package:openskp/src/geometry.dart';
import 'package:openskp/src/tlv.dart';
import 'package:test/test.dart';

Uint8List doubles(List<double> values) {
  final bytes = Uint8List(values.length * 8);
  final view = ByteData.sublistView(bytes);
  for (var i = 0; i < values.length; i++) {
    view.setFloat64(i * 8, values[i], Endian.little);
  }
  return bytes;
}

TlvNode node(String tag, {List<TlvNode>? children, Uint8List? payload}) =>
    TlvNode(offset: 0, tag: tag, size: payload?.length ?? 0,
        children: children, payload: payload);

void main() {
  group('VFF construction guide geometry (#285)', () {
    test('the TLV walker recognizes guide lists and entity wrappers', () {
      for (final tag in ['9113', '9213', '6942', '6C42', '6842']) {
        expect(Tlv.containerTags.contains(tag), isTrue);
      }
    });

    test('extracts finite and infinite lines plus construction points', () {
      final lines = node('9113', children: [
        node('6942', children: [
          node('6A42', payload: doubles([2, 3, 4, 0, 1, 0, -2, 7])),
        ]),
        node('6942', children: [
          node('6A42', payload: doubles([0, 0, 4, 0, 0, 1, -1e30, 1e30])),
        ]),
      ]);
      final points = node('9213', children: [
        node('6C42', children: [
          node('6D42', payload: doubles([8, 9, 10])),
          node('6E42', payload: doubles([0, 0, 0])),
          node('6F42', payload: Uint8List.fromList([0])),
        ]),
      ]);
      final builder = GeometryBuilder();
      Geometry.extractGeometryFromNodes([lines, points], builder);

      expect(builder.constructionLines, hasLength(2));
      expect(builder.constructionLines[0].point, (2.0, 3.0, 4.0));
      expect(builder.constructionLines[0].direction, (0.0, 1.0, 0.0));
      expect(builder.constructionLines[0].start, (2.0, 1.0, 4.0));
      expect(builder.constructionLines[0].end, (2.0, 10.0, 4.0));
      expect(builder.constructionLines[1].start, isNull);
      expect(builder.constructionLines[1].end, isNull);
      expect(builder.constructionPoints.single.position, (8.0, 9.0, 10.0));
    });

    test('ignores incomplete line and point payloads', () {
      final builder = GeometryBuilder();
      Geometry.extractGeometryFromNodes([
        node('6942', children: [
          node('6A42', payload: doubles([1, 2, 3])),
        ]),
        node('6C42', children: [
          node('6D42', payload: doubles([1, 2])),
        ]),
      ], builder);
      expect(builder.constructionLines, isEmpty);
      expect(builder.constructionPoints, isEmpty);
    });
  });
}
