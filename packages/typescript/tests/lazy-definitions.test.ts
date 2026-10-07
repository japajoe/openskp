import { describe, it, expect } from 'vitest';
import { iterTopLevelLazy, parseTlvRecursive, TlvNode } from '../src/parser';

/**
 * iterTopLevelLazy streams the F901 > 7017 > 7117 definitions chain one
 * "7C15" definition at a time.
 *
 * In real SketchUp 2021+ files that chain is nearly the whole model: a
 * 70 MB .skp decompressed to a 297 MB model.dat with 100% of its bytes under
 * one F901, holding 1,203 definitions (largest 0.8 MB). Yielding F901 as a
 * single record built the whole definitions tree at once, and parsing that
 * file needed more than 3 GB of heap; streaming each definition reads it
 * with 1-1.5 GB and an identical result.
 */

/** Build a single TLV element: 2-byte tag (hex) + 4-byte LE size + payload. */
function tlv(tagHex: string, payload: Uint8Array): Uint8Array {
  const tagBytes = new Uint8Array(tagHex.match(/.{2}/g)!.map((h) => parseInt(h, 16)));
  const sizeBytes = new Uint8Array(4);
  new DataView(sizeBytes.buffer).setUint32(0, payload.length, true);
  const result = new Uint8Array(2 + 4 + payload.length);
  result.set(tagBytes, 0);
  result.set(sizeBytes, 2);
  result.set(payload, 6);
  return result;
}

function concatBytes(...arrays: Uint8Array[]): Uint8Array {
  const total = arrays.reduce((sum, a) => sum + a.length, 0);
  const result = new Uint8Array(total);
  let offset = 0;
  for (const a of arrays) {
    result.set(a, offset);
    offset += a.length;
  }
  return result;
}

const utf8 = (s: string) => new TextEncoder().encode(s);

/** A "7C15" definition with a 16-byte GUID (7D15) and a name (7E15). */
function definition(seed: number, name: string): Uint8Array {
  return tlv('7C15', concatBytes(tlv('7D15', new Uint8Array(16).fill(seed)), tlv('7E15', utf8(name))));
}

/** A "993A" layers container, which is NOT a pass-through wrapper. */
const layers = tlv('993A', tlv('8C3C', tlv('8D3C', utf8('Layer0'))));

function model(...defs: Uint8Array[]): Uint8Array {
  return concatBytes(
    layers,
    tlv('F901', tlv('7017', tlv('7117', concatBytes(...defs)))),
    tlv('0802', new Uint8Array([1, 2, 3]))
  );
}

function collect(data: Uint8Array) {
  return [...iterTopLevelLazy(data, 0, data.length)];
}

/** The 7C15 nodes of a full (non-streamed) parse, for comparison. */
function definitionsFromFullParse(data: Uint8Array): TlvNode[] {
  const found: TlvNode[] = [];
  const walk = (nodes: TlvNode[]) => {
    for (const n of nodes) {
      if (n.tag === '7C15') found.push(n);
      else walk(n.children);
    }
  };
  walk(parseTlvRecursive(data, 0, data.length));
  return found;
}

describe('iterTopLevelLazy - F901 > 7017 > 7117 definitions are streamed one at a time', () => {
  it('yields each 7C15 as its own record, in file order, between its siblings', () => {
    const data = model(definition(1, 'Porta'), definition(2, 'Gaveta'), definition(3, 'Prateleira'));
    const records = collect(data);

    expect(records.map((r) => r.node.tag)).toEqual(['993A', '7C15', '7C15', '7C15', '0802']);
    expect(records.map((r) => r.index)).toEqual([0, 1, 2, 3, 4]);
    expect(records.every((r) => r.total === 5)).toBe(true);
  });

  it('never yields one of the pass-through wrappers', () => {
    const records = collect(model(definition(1, 'A'), definition(2, 'B')));
    const tags = new Set<string>();
    const walk = (n: TlvNode) => {
      tags.add(n.tag);
      n.children.forEach(walk);
    };
    records.forEach((r) => walk(r.node));

    expect(tags.has('F901')).toBe(false);
    expect(tags.has('7017')).toBe(false);
    expect(tags.has('7117')).toBe(false);
  });

  it('each streamed definition is identical to the one a full parse builds', () => {
    const data = model(definition(1, 'Porta'), definition(2, 'Gaveta'), definition(3, 'Prateleira'));
    const streamed = collect(data)
      .map((r) => r.node)
      .filter((n) => n.tag === '7C15');

    expect(streamed).toEqual(definitionsFromFullParse(data));
  });

  it('leaves every other container whole', () => {
    const [first] = collect(model(definition(1, 'A')));

    expect(first.node.tag).toBe('993A');
    expect(first.node.children.map((c) => c.tag)).toEqual(['8C3C']);
    expect(first.node.children[0].children.map((c) => c.tag)).toEqual(['8D3C']);
  });

  it('still unwraps a lone F401 before streaming the definitions', () => {
    const inner = model(definition(1, 'A'), definition(2, 'B'));
    const records = collect(tlv('F401', inner));

    expect(records.map((r) => r.node.tag)).toEqual(['993A', '7C15', '7C15', '0802']);
  });

  it('an empty definitions chain yields nothing for it', () => {
    const records = collect(model());

    expect(records.map((r) => r.node.tag)).toEqual(['993A', '0802']);
  });
});
