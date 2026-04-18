//using Krizaljka.Domain.Terms;

//namespace Krizaljka.Domain.Creator;

//internal sealed class CreatorCache
//{
//    /*
//     Length
//        -> Position
//            -> Letter
//                -> Terms
//     *
//     *
//     */

//    //public IReadOnlyDictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>> TermsByLengthPositionLetter
//    //    = new Dictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>>();

//    public Dictionary<(int SlotId, string Pattern), IReadOnlyList<Term>> MatchingTermsCache { get; } = [];

//    //public Dictionary<(int SlotId, string Pattern), int> FittingCountCache = [];
//}
