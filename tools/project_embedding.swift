import Foundation
import NaturalLanguage
struct Request: Decodable { let texts: [String] }
struct Response: Encodable { let model: String; let dimensions: Int; let vectors: [[Double]] }
do {
    guard let e = NLEmbedding.sentenceEmbedding(for: .simplifiedChinese) else {
        throw NSError(domain: "ProjectMemory", code: 1,
                      userInfo: [NSLocalizedDescriptionKey: "Chinese sentence embedding unavailable"])
    }
    let input = try JSONDecoder().decode(Request.self, from: FileHandle.standardInput.readDataToEndOfFile())
    let vectors = try input.texts.map { text -> [Double] in
        guard let v = e.vector(for: text) else {
            throw NSError(domain: "ProjectMemory", code: 2,
                          userInfo: [NSLocalizedDescriptionKey: "Cannot embed text"])
        }
        return v
    }
    let response = Response(model: "apple-natural-language-zh-Hans-sentence-r\(e.revision)", dimensions: e.dimension, vectors: vectors)
    FileHandle.standardOutput.write(try JSONEncoder().encode(response))
} catch {
    FileHandle.standardError.write(Data("\(error)\n".utf8)); exit(1)
}
