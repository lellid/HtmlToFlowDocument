﻿using System;
using System.Collections.Generic;

namespace ExCSS
{
    internal sealed class StylesheetComposer
    {
        private readonly Lexer _lexer;
        private readonly StylesheetParser _parser;
        private readonly Stack<StylesheetNode> _nodes;

        public StylesheetComposer(Lexer lexer, StylesheetParser parser)
        {
            _lexer = lexer;
            _parser = parser;
            _nodes = new Stack<StylesheetNode>();
        }

        public Rule CreateAtRule(Token token)
        {
            if (token.Data.Is(RuleNames.Media))
            {
                return CreateMedia(token);
            }

            if (token.Data.Is(RuleNames.FontFace))
            {
                return CreateFontFace(token);
            }

            if (token.Data.Is(RuleNames.Keyframes))
            {
                return CreateKeyframes(token);
            }

            if (token.Data.Is(RuleNames.Import))
            {
                return CreateImport(token);
            }

            if (token.Data.Is(RuleNames.Charset))
            {
                return CreateCharset(token);
            }

            if (token.Data.Is(RuleNames.Namespace))
            {
                return CreateNamespace(token);
            }

            if (token.Data.Is(RuleNames.Page))
            {
                return CreatePage(token);
            }

            if (token.Data.Is(RuleNames.Supports))
            {
                return CreateSupports(token);
            }

            if (token.Data.Is(RuleNames.ViewPort))
            {
                return CreateViewport(token);
            }

            return token.Data.Is(RuleNames.Document) ? CreateDocument(token) : CreateUnknown(token);
        }

        public Rule CreateRule(Token token)
        {
            switch (token.Type)
            {
                case TokenType.AtKeyword:
                    return CreateAtRule(token);

                case TokenType.CurlyBracketOpen:
                    RaiseErrorOccurred(ParseError.InvalidBlockStart, token.Position);
                    MoveToRuleEnd(ref token);
                    return null;

                case TokenType.String:
                case TokenType.Url:
                case TokenType.CurlyBracketClose:
                case TokenType.RoundBracketClose:
                case TokenType.SquareBracketClose:
                    RaiseErrorOccurred(ParseError.InvalidToken, token.Position);
                    MoveToRuleEnd(ref token);
                    return null;

                default:
                    return CreateStyle(token);
            }
        }

        public Rule CreateCharset(Token current)
        {
            var rule = new CharsetRule(_parser);
            var start = current.Position;
            var token = NextToken();
            _nodes.Push(rule);
            ParseComments(ref token);

            if (token.Type == TokenType.String)
            {
                rule.CharacterSet = token.Data;
            }

            JumpToEnd(ref token);
            rule.StylesheetText = CreateView(start, token.Position);
            _nodes.Pop();
            return rule;
        }

        public Rule CreateDocument(Token current)
        {
            var rule = new DocumentRule(_parser);
            var start = current.Position;
            var token = NextToken();
            _nodes.Push(rule);
            ParseComments(ref token);
            FillFunctions(function => rule.AppendChild(function), ref token);
            ParseComments(ref token);

            if (token.Type == TokenType.CurlyBracketOpen)
            {
                var end = FillRules(rule);
                rule.StylesheetText = CreateView(start, end);
                _nodes.Pop();
                return rule;
            }

            _nodes.Pop();
            return SkipDeclarations(token);
        }

        public Rule CreateViewport(Token current)
        {
            var rule = new ViewportRule(_parser);
            var start = current.Position;
            var token = NextToken();
            _nodes.Push(rule);
            ParseComments(ref token);

            if (token.Type == TokenType.CurlyBracketOpen)
            {
                var end = FillDeclarations(rule, PropertyFactory.Instance.CreateViewport);

                rule.StylesheetText = CreateView(start, end);
                _nodes.Pop();
                return rule;
            }

            _nodes.Pop();
            return SkipDeclarations(token);
        }

        public Rule CreateFontFace(Token current)
        {
            var rule = new FontFaceRule(_parser);
            var start = current.Position;
            var token = NextToken();
            _nodes.Push(rule);
            ParseComments(ref token);

            if (token.Type == TokenType.CurlyBracketOpen)
            {
                var end = FillDeclarations(rule, PropertyFactory.Instance.CreateFont);
                rule.StylesheetText = CreateView(start, end);
                _nodes.Pop();
                return rule;
            }

            _nodes.Pop();
            return SkipDeclarations(token);
        }

        public Rule CreateImport(Token current)
        {
            var rule = new ImportRule(_parser);
            var start = current.Position;
            var token = NextToken();
            _nodes.Push(rule);
            ParseComments(ref token);

            if (token.Is(TokenType.String, TokenType.Url))
            {
                rule.Href = token.Data;
                token = NextToken();
                ParseComments(ref token);
                FillMediaList(rule.Media, TokenType.Semicolon, ref token);
            }

            ParseComments(ref token);
            JumpToEnd(ref token);
            rule.StylesheetText = CreateView(start, token.Position);
            _nodes.Pop();
            return rule;
        }

        public Rule CreateKeyframes(Token current)
        {
            var rule = new KeyframesRule(_parser);
            var start = current.Position;
            var token = NextToken();
            _nodes.Push(rule);
            ParseComments(ref token);
            rule.Name = GetRuleName(ref token);
            ParseComments(ref token);

            if (token.Type == TokenType.CurlyBracketOpen)
            {
                var end = FillKeyframeRules(rule);
                rule.StylesheetText = CreateView(start, end);
                _nodes.Pop();
                return rule;
            }

            _nodes.Pop();
            return SkipDeclarations(token);
        }

        public Rule CreateMedia(Token current)
        {
            var rule = new MediaRule(_parser);
            var start = current.Position;
            var token = NextToken();
            _nodes.Push(rule);
            ParseComments(ref token);
            FillMediaList(rule.Media, TokenType.CurlyBracketOpen, ref token);
            ParseComments(ref token);

            if (token.Type != TokenType.CurlyBracketOpen)
            {
                while (token.Type != TokenType.EndOfFile)
                {
                    if (token.Type == TokenType.Semicolon)
                    {
                        _nodes.Pop();
                        return null;
                    }
                    if (token.Type == TokenType.CurlyBracketOpen)
                    {
                        break;
                    }

                    token = NextToken();
                }
            }

            var end = FillRules(rule);
            rule.StylesheetText = CreateView(start, end);
            _nodes.Pop();
            return rule;
        }

        public Rule CreateNamespace(Token current)
        {
            var rule = new NamespaceRule(_parser);
            var start = current.Position;
            var token = NextToken();
            _nodes.Push(rule);
            ParseComments(ref token);
            rule.Prefix = GetRuleName(ref token);
            ParseComments(ref token);

            if (token.Type == TokenType.Url)
            {
                rule.NamespaceUri = token.Data;
            }

            JumpToEnd(ref token);
            rule.StylesheetText = CreateView(start, token.Position);
            _nodes.Pop();
            return rule;
        }

        public Rule CreatePage(Token current)
        {
            var rule = new PageRule(_parser);
            var start = current.Position;
            var token = NextToken();
            _nodes.Push(rule);
            ParseComments(ref token);
            rule.Selector = CreateSelector(ref token);
            ParseComments(ref token);

            if (token.Type == TokenType.CurlyBracketOpen)
            {
                var end = FillDeclarations(rule.Style);
                rule.StylesheetText = CreateView(start, end);
                _nodes.Pop();
                return rule;
            }

            _nodes.Pop();
            return SkipDeclarations(token);
        }

        public Rule CreateSupports(Token current)
        {
            var rule = new SupportsRule(_parser);
            var start = current.Position;
            var token = NextToken();
            _nodes.Push(rule);
            ParseComments(ref token);
            rule.Condition = AggregateCondition(ref token);
            ParseComments(ref token);

            if (token.Type == TokenType.CurlyBracketOpen)
            {
                var end = FillRules(rule);
                rule.StylesheetText = CreateView(start, end);
                _nodes.Pop();
                return rule;
            }

            _nodes.Pop();
            return SkipDeclarations(token);
        }

        public Rule CreateStyle(Token current)
        {
            var rule = new StyleRule(_parser);
            var start = current.Position;
            _nodes.Push(rule);
            ParseComments(ref current);
            rule.Selector = CreateSelector(ref current);
            var end = FillDeclarations(rule.Style);
            rule.StylesheetText = CreateView(start, end);
            _nodes.Pop();
            return rule.Selector != null ? rule : null;
        }

        public KeyframeRule CreateKeyframeRule(Token current)
        {
            var rule = new KeyframeRule(_parser);
            var start = current.Position;
            _nodes.Push(rule);
            ParseComments(ref current);
            rule.Key = CreateKeyframeSelector(ref current);
            var end = FillDeclarations(rule.Style);
            rule.StylesheetText = CreateView(start, end);
            _nodes.Pop();
            return rule.Key != null ? rule : null;
        }

        public Rule CreateUnknown(Token current)
        {
            var start = current.Position;

            if (_parser.Options.IncludeUnknownRules)
            {
                var token = NextToken();
                var rule = new UnknownRule(current.Data, _parser);
                _nodes.Push(rule);

                while (token.IsNot(TokenType.CurlyBracketOpen, TokenType.Semicolon, TokenType.EndOfFile))
                {
                    token = NextToken();
                }

                if (token.Type == TokenType.CurlyBracketOpen)
                {
                    var curly = 1;

                    do
                    {
                        token = NextToken();

                        switch (token.Type)
                        {
                            case TokenType.CurlyBracketOpen:
                                curly++;
                                break;
                            case TokenType.CurlyBracketClose:
                                curly--;
                                break;
                            case TokenType.EndOfFile:
                                curly = 0;
                                break;
                        }
                    } while (curly != 0);
                }

                rule.StylesheetText = CreateView(start, token.Position);
                _nodes.Pop();
                return rule;
            }
            RaiseErrorOccurred(ParseError.UnknownAtRule, start);
            MoveToRuleEnd(ref current);
            return default(UnknownRule);
        }

        public TokenValue CreateValue(ref Token token)
        {
            bool important;
            return CreateValue(TokenType.CurlyBracketClose, ref token, out important);
        }

        public List<Medium> CreateMedia(ref Token token)
        {
            var list = new List<Medium>();
            ParseComments(ref token);

            while (token.Type != TokenType.EndOfFile)
            {
                var medium = CreateMedium(ref token);

                if ((medium == null) || token.IsNot(TokenType.Comma, TokenType.EndOfFile))
                {
                    throw new ParseException("Unable to create medium or end of file reached unexpectedly");
                }

                token = NextToken();
                ParseComments(ref token);
                list.Add(medium);
            }

            return list;
        }

        public TextPosition CreateRules(Stylesheet sheet)
        {
            var token = NextToken();
            _nodes.Push(sheet);
            ParseComments(ref token);

            while (token.Type != TokenType.EndOfFile)
            {
                var rule = CreateRule(token);
                token = NextToken();
                ParseComments(ref token);
                sheet.Rules.Add(rule);
            }

            _nodes.Pop();
            return token.Position;
        }

        public IConditionFunction CreateCondition(ref Token token)
        {
            ParseComments(ref token);
            return AggregateCondition(ref token);
        }

        public KeyframeSelector CreateKeyframeSelector(ref Token token)
        {
            var keys = new List<Percent>();
            var valid = true;
            var start = token.Position;
            ParseComments(ref token);

            while (token.Type != TokenType.EndOfFile)
            {
                if (keys.Count > 0)
                {
                    if (token.Type == TokenType.CurlyBracketOpen)
                    {
                        break;
                    }
                    if (token.Type != TokenType.Comma)
                    {
                        valid = false;
                    }
                    else
                    {
                        token = NextToken();
                    }

                    ParseComments(ref token);
                }

                if (token.Type == TokenType.Percentage)
                {
                    keys.Add(new Percent(((UnitToken) token).Value));
                }
                else if ((token.Type == TokenType.Ident) && token.Data.Is(Keywords.From))
                {
                    keys.Add(Percent.Zero);
                }
                else if ((token.Type == TokenType.Ident) && token.Data.Is(Keywords.To))
                {
                    keys.Add(Percent.Hundred);
                }
                else
                {
                    valid = false;
                }

                token = NextToken();
                ParseComments(ref token);
            }

            if (!valid)
            {
                RaiseErrorOccurred(ParseError.InvalidSelector, start);
            }

            return new KeyframeSelector(keys);
        }

        public List<DocumentFunction> CreateFunctions(ref Token token)
        {
            var functions = new List<DocumentFunction>();
            ParseComments(ref token);
            FillFunctions(function => functions.Add(function), ref token);
            return functions;
        }

        public TextPosition FillDeclarations(StyleDeclaration style)
        {
            var finalProperties = new Dictionary<string, IProperty>(StringComparer.OrdinalIgnoreCase);
            var token = NextToken();
            _nodes.Push(style);
            ParseComments(ref token);

            while (token.IsNot(TokenType.EndOfFile, TokenType.CurlyBracketClose))
            {
                var sourceProperty = CreateDeclarationWith(PropertyFactory.Instance.Create, ref token);
                var resolvedProperties = new[] { sourceProperty };

                if ((sourceProperty != null) && sourceProperty.HasValue)
                {
                    // For shorthand properties we need to first find out what alternate set of properties they will
                    // end up resolving into so that we can compare them with their previously parsed counterparts (if any)
                    // and determine which one takes priority over the other.
                    // Example 1: "margin-left: 5px !important; text-align:center; margin: 3px;";
                    // Example 2: "margin: 5px !important; text-align:center; margin-left: 3px;";
                    if (sourceProperty is ShorthandProperty)
                    {
                        var shorthandProperty = sourceProperty as ShorthandProperty;
                        resolvedProperties = PropertyFactory.Instance.CreateLonghandsFor(shorthandProperty.Name);
                        shorthandProperty.Export(resolvedProperties);
                    }

                    foreach (var resolvedProperty in resolvedProperties)
                    {
                        // The following relies on the fact that the tokens are processed in 
                        // top-to-bottom order of how they are defined in the parsed style declaration.
                        // This handles exposing the correct value for a property when it appears multiple 
                        // times in the same style declaration.
                        // Example: "background-color:green !important; text-align:center; background-color:yellow;";
                        // In this example even though background-color yellow is defined last, the previous value
                        // of green should be he one exposed given it is tagged as important.
                        // ------------------------------------------------------------------------------------------
                        // Only set this property if one of the following conditions is true:
                        // a) It was not previously added or...
                        // b) The previously added property is not tagged as important or ...
                        // c) The previously added property is tagged as important but so is this new one.
                        var shouldSetProperty =
                            !finalProperties.TryGetValue(resolvedProperty.Name, out var previousProperty)
                            || !previousProperty.IsImportant
                            || resolvedProperty.IsImportant;

                        if (shouldSetProperty)
                        {
                            style.SetProperty(resolvedProperty);
                            finalProperties[resolvedProperty.Name] = resolvedProperty;
                        }
                    }
                }

                ParseComments(ref token);
            }

            _nodes.Pop();
            return token.Position;
        }

        public Property CreateDeclarationWith(Func<string, Property> createProperty, ref Token token)
        {
            var property = default(Property);

            var sb = Pool.NewStringBuilder();
            var start = token.Position;

            while (token.IsDeclarationName())
            {
                sb.Append(token.ToValue());
                token = NextToken();
            }

            var propertyName = sb.ToPool();

            if (propertyName.Length > 0)
            {
                property = _parser.Options.IncludeUnknownDeclarations ||
                           _parser.Options.AllowInvalidValues
                    ? new UnknownProperty(propertyName)
                    : createProperty(propertyName);

                if (property == null)
                {
                    RaiseErrorOccurred(ParseError.UnknownDeclarationName, start);
                }
                else
                {
                    _nodes.Push(property);
                }

                ParseComments(ref token);

                if (token.Type == TokenType.Colon)
                {
                    bool important;
                    var value = CreateValue(TokenType.CurlyBracketClose, ref token, out important);

                    if (value == null)
                    {
                        RaiseErrorOccurred(ParseError.ValueMissing, token.Position);
                    }
                    else if ((property != null) && property.TrySetValue(value))
                    {
                        property.IsImportant = important;
                    }

                    ParseComments(ref token);
                }
                else
                {
                    RaiseErrorOccurred(ParseError.ColonMissing, token.Position);
                }

                JumpToDeclEnd(ref token);

                if (property != null)
                {
                    _nodes.Pop();
                }
            }
            else if (token.Type != TokenType.EndOfFile)
            {
                RaiseErrorOccurred(ParseError.IdentExpected, start);
                JumpToDeclEnd(ref token);
            }

            if (token.Type == TokenType.Semicolon)
            {
                token = NextToken();
            }

            return property;
        }

        public Property CreateDeclaration(ref Token token)
        {
            ParseComments(ref token);
            return CreateDeclarationWith(PropertyFactory.Instance.Create, ref token);
        }

        public Medium CreateMedium(ref Token token)
        {
            var medium = new Medium();
            ParseComments(ref token);

            if (token.Type == TokenType.Ident)
            {
                var identifier = token.Data;

                if (identifier.Isi(Keywords.Not))
                {
                    medium.IsInverse = true;
                    token = NextToken();
                    ParseComments(ref token);
                }
                else if (identifier.Isi(Keywords.Only))
                {
                    medium.IsExclusive = true;
                    token = NextToken();
                    ParseComments(ref token);
                }
            }

            if (token.Type == TokenType.Ident)
            {
                medium.Type = token.Data;
                token = NextToken();
                ParseComments(ref token);

                if ((token.Type != TokenType.Ident) || !token.Data.Isi(Keywords.And))
                {
                    return medium;
                }

                token = NextToken();
                ParseComments(ref token);
            }

            do
            {
                if (token.Type != TokenType.RoundBracketOpen)
                {
                    return null;
                }

                token = NextToken();
                ParseComments(ref token);
                var feature = CreateFeature(ref token);

                if (feature != null)
                {
                    medium.AppendChild(feature);
                }

                if (token.Type != TokenType.RoundBracketClose)
                {
                    return null;
                }

                token = NextToken();
                ParseComments(ref token);

                if (feature == null)
                {
                    return null;
                }

                if ((token.Type != TokenType.Ident) || !token.Data.Isi(Keywords.And))
                {
                    break;
                }

                token = NextToken();
                ParseComments(ref token);
            } while (token.Type != TokenType.EndOfFile);

            return medium;
        }

        private void JumpToEnd(ref Token current)
        {
            while (current.IsNot(TokenType.EndOfFile, TokenType.Semicolon))
            {
                current = NextToken();
            }
        }

        private void MoveToRuleEnd(ref Token current)
        {
            var scopes = 0;

            while (current.Type != TokenType.EndOfFile)
            {
                if (current.Type == TokenType.CurlyBracketOpen)
                {
                    scopes++;
                }
                else if (current.Type == TokenType.CurlyBracketClose)
                {
                    scopes--;
                }

                if ((scopes <= 0) && current.Is(TokenType.CurlyBracketClose, TokenType.Semicolon))
                {
                    break;
                }

                current = NextToken();
            }
        }

        private void JumpToArgEnd(ref Token current)
        {
            var arguments = 0;

            while (current.Type != TokenType.EndOfFile)
            {
                if (current.Type == TokenType.RoundBracketOpen)
                {
                    arguments++;
                }
                else if ((arguments <= 0) && (current.Type == TokenType.RoundBracketClose))
                {
                    break;
                }
                else if (current.Type == TokenType.RoundBracketClose)
                {
                    arguments--;
                }

                current = NextToken();
            }
        }

        private void JumpToDeclEnd(ref Token current)
        {
            var scopes = 0;

            while (current.Type != TokenType.EndOfFile)
            {
                if (current.Type == TokenType.CurlyBracketOpen)
                {
                    scopes++;
                }
                else if ((scopes <= 0) && current.Is(TokenType.CurlyBracketClose, TokenType.Semicolon))
                {
                    break;
                }
                else if (current.Type == TokenType.CurlyBracketClose)
                {
                    scopes--;
                }

                current = NextToken();
            }
        }

        private Token NextToken()
        {
            return _lexer.Get();
        }

        private StylesheetText CreateView(TextPosition start, TextPosition end)
        {
            var range = new TextRange(start, end);
            return new StylesheetText(range, _lexer.Source);
        }

        private void ParseComments(ref Token token)
        {
            var preserveComments = _parser.Options.PreserveComments;

            while ((token.Type == TokenType.Whitespace) || (token.Type == TokenType.Comment) ||
                   (token.Type == TokenType.Cdc) || (token.Type == TokenType.Cdo))
            {
                if (preserveComments && (token.Type == TokenType.Comment))
                {
                    var current = _nodes.Peek();
                    var comment = new Comment(token.Data);
                    var start = token.Position;
                    var end = start.After(token.ToValue());
                    comment.StylesheetText = CreateView(start, end);
                    current.AppendChild(comment);
                }

                token = _lexer.Get();
            }
        }

        private Rule SkipDeclarations(Token token)
        {
            RaiseErrorOccurred(ParseError.InvalidToken, token.Position);
            MoveToRuleEnd(ref token);
            return default(Rule);
        }

        private void RaiseErrorOccurred(ParseError code, TextPosition position)
        {
            _lexer.RaiseErrorOccurred(code, position);
        }

        private IConditionFunction AggregateCondition(ref Token token)
        {
            var condition = ExtractCondition(ref token);

            if (condition == null)
            {
                return null;
            }

            ParseComments(ref token);
            var conjunction = token.Data;
            var creator = conjunction.GetCreator();

            if (creator != null)
            {
                token = NextToken();
                ParseComments(ref token);
                var conditions = MultipleConditions(condition, conjunction, ref token);
                condition = creator.Invoke(conditions);
            }

            return condition;
        }

        private IConditionFunction ExtractCondition(ref Token token)
        {
            if (token.Type == TokenType.RoundBracketOpen)
            {
                token = NextToken();
                ParseComments(ref token);
                var condition = AggregateCondition(ref token);

                if (condition != null)
                {
                    var group = new GroupCondition();
                    group.Content = condition;
                    condition = group;
                }
                else if (token.Type == TokenType.Ident)
                {
                    condition = DeclarationCondition(ref token);
                }

                if (token.Type != TokenType.RoundBracketClose)
                {
                    return condition;
                }
                token = NextToken();
                ParseComments(ref token);

                return condition;
            }
            if (token.Data.Isi(Keywords.Not))
            {
                var condition = new NotCondition();
                token = NextToken();
                ParseComments(ref token);
                condition.Content = ExtractCondition(ref token);
                return condition;
            }

            return null;
        }

        private IConditionFunction DeclarationCondition(ref Token token)
        {
            var property = PropertyFactory.Instance.Create(token.Data) ?? new UnknownProperty(token.Data);
            var declaration = default(DeclarationCondition);
            token = NextToken();
            ParseComments(ref token);

            if (token.Type != TokenType.Colon)
            {
                return null;
            }

            bool important;
            var result = CreateValue(TokenType.RoundBracketClose, ref token, out important);
            property.IsImportant = important;

            if (result != null)
            {
                declaration = new DeclarationCondition(property, result);
            }

            return declaration;
        }

        private List<IConditionFunction> MultipleConditions(IConditionFunction condition, string connector,ref Token token)
        {
            var list = new List<IConditionFunction>();
            ParseComments(ref token);
            list.Add(condition);

            while (token.Type != TokenType.EndOfFile)
            {
                condition = ExtractCondition(ref token);

                if (condition == null)
                {
                    break;
                }

                list.Add(condition);

                if (!token.Data.Isi(connector))
                {
                    break;
                }

                token = NextToken();
                ParseComments(ref token);
            }

            return list;
        }

        private void FillFunctions(Action<DocumentFunction> add, ref Token token)
        {
            do
            {
                var function = token.ToDocumentFunction();

                if (function == null)
                {
                    break;
                }

                token = NextToken();
                ParseComments(ref token);
                add(function);

                if (token.Type != TokenType.Comma)
                {
                    break;
                }

                token = NextToken();
                ParseComments(ref token);
            } while (token.Type != TokenType.EndOfFile);
        }

        private TextPosition FillKeyframeRules(KeyframesRule parentRule)
        {
            var token = NextToken();
            ParseComments(ref token);

            while (token.IsNot(TokenType.EndOfFile, TokenType.CurlyBracketClose))
            {
                var rule = CreateKeyframeRule(token);
                token = NextToken();
                ParseComments(ref token);
                parentRule.Rules.Add(rule);
            }

            return token.Position;
        }

        private TextPosition FillDeclarations(DeclarationRule rule, Func<string, Property> createProperty)
        {
            var token = NextToken();
            ParseComments(ref token);

            while (token.IsNot(TokenType.EndOfFile, TokenType.CurlyBracketClose))
            {
                var property = CreateDeclarationWith(createProperty, ref token);

                if ((property != null) && property.HasValue)
                    rule.SetProperty(property);

                ParseComments(ref token);
            }

            return token.Position;
        }

        private TextPosition FillRules(GroupingRule group)
        {
            var token = NextToken();
            ParseComments(ref token);

            while (token.IsNot(TokenType.EndOfFile, TokenType.CurlyBracketClose))
            {
                var rule = CreateRule(token);
                token = NextToken();
                ParseComments(ref token);
                group.Rules.Add(rule);
            }

            return token.Position;
        }

        private void FillMediaList(MediaList list, TokenType end, ref Token token)
        {
            _nodes.Push(list);

            if (token.Type != end)
            {
                while (token.Type != TokenType.EndOfFile)
                {
                    var medium = CreateMedium(ref token);

                    if (medium != null)
                    {
                        list.AppendChild(medium);
                    }

                    if (token.Type != TokenType.Comma)
                    {
                        break;
                    }

                    token = NextToken();
                    ParseComments(ref token);
                }

                if ((token.Type != end) || (list.Length == 0))
                {
                    list.Clear();
                    list.AppendChild(new Medium
                    {
                        IsInverse = true,
                        Type = Keywords.All
                    });
                }
            }

            _nodes.Pop();
        }

        private ISelector CreateSelector(ref Token token)
        {
            var selector = _parser.GetSelectorCreator();
            var start = token.Position;

            while (token.IsNot(TokenType.EndOfFile, TokenType.CurlyBracketOpen, TokenType.CurlyBracketClose))
            {
                selector.Apply(token);
                token = NextToken();
            }

            var selectorIsValid = selector.IsValid;
            var result = selector.ToPool();
            var node = result as StylesheetNode;

            if (node != null)
            {
                var end = token.Position.Shift(-1);
                node.StylesheetText = CreateView(start, end);
            }

            if (!selectorIsValid && !_parser.Options.AllowInvalidValues)
            {
                RaiseErrorOccurred(ParseError.InvalidSelector, start);
                result = null;
            }

            return result;
        }

        private TokenValue CreateValue(TokenType closing, ref Token token, out bool important)
        {
            var value = Pool.NewValueBuilder();
            _lexer.IsInValue = true;
            token = NextToken();
            var start = token.Position;

            while (token.IsNot(TokenType.EndOfFile, TokenType.Semicolon, closing))
            {
                value.Apply(token);
                token = NextToken();
            }

            important = value.IsImportant;
            _lexer.IsInValue = false;
            var valueIsValid = value.IsValid;
            var result = value.ToPool();

            //var node = result as StylesheetNode;
            var node = (StylesheetNode) result;

            if (node != null)
            {
                var end = token.Position.Shift(-1);
                node.StylesheetText = CreateView(start, end);
            }

            if (!valueIsValid && !_parser.Options.AllowInvalidValues)
            {
                RaiseErrorOccurred(ParseError.InvalidValue, start);
                result = null;
            }

            return result;
        }

        private string GetRuleName(ref Token token)
        {
            var name = string.Empty;

            if (token.Type == TokenType.Ident)
            {
                name = token.Data;
                token = NextToken();
            }

            return name;
        }

        private MediaFeature CreateFeature(ref Token token)
        {
            if (token.Type == TokenType.Ident)
            {
                var start = token.Position;
                var val = TokenValue.Empty;
                var feature = _parser.Options.AllowInvalidConstraints
                    ? new UnknownMediaFeature(token.Data)
                    : MediaFeatureFactory.Instance.Create(token.Data);

                token = NextToken();

                if (token.Type == TokenType.Colon)
                {
                    var value = Pool.NewValueBuilder();
                    token = NextToken();

                    while (token.IsNot(TokenType.RoundBracketClose, TokenType.EndOfFile) || !value.IsReady)
                    {
                        value.Apply(token);
                        token = NextToken();
                    }

                    val = value.ToPool();
                }
                else if (token.Type == TokenType.EndOfFile)
                {
                    return null;
                }

                if ((feature != null) && feature.TrySetValue(val))
                {
                    var node = feature as StylesheetNode;

                    if (node != null)
                    {
                        var end = token.Position.Shift(-1);
                        node.StylesheetText = CreateView(start, end);
                    }

                    return feature;
                }
            }
            else
            {
                JumpToArgEnd(ref token);
            }

            return null;
        }
    }
}