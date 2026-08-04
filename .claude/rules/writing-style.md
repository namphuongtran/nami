# Writing style

Write so a reader whose first language is not English can follow you on the first read.
These rules come from the Microsoft Style Guide. They apply to every document in this
repository, to commit messages, and to plans.

**One rule outranks every rule below: never trade accuracy for simplicity.** If the simpler
sentence is less exact, keep the exact one and split it into two sentences instead. A
sentence that is easy to read and wrong is worse than a hard one. The reader cannot tell the
two apart.

The sections down to "Nami only" hold no Nami detail, so they copy to any project unchanged.
This file carries no `paths` field, so it loads in every session.

## Shape the answer

- **Answer first.** Give the result in 1 to 3 short lines. Put reasons, evidence, and file
  references below that.
- **Use lists and tables, not paragraphs.** Steps become a numbered list. A comparison
  becomes a table.
- **Put the most important thing where the reader looks first.**

## Shape the sentence

- **One idea per sentence.** Keep sentences under about 20 words.
- **Split, do not join.** Two short sentences are better than one long sentence. Do not
  join two ideas with a comma.
- **Link at most two clauses** with "and", "or", or "but".
- **Use standard word order:** subject, then verb, then object.
- **Use active voice.** Use the imperative for steps. Write "Run the tests", not "The tests
  should be run".
- **Keep "that", "who", "the", and "a".** They tell the reader where the sentence turns.
  Write "verify that all tables were migrated", not "verify all tables were migrated".

## Choose the word

- **Use the shorter word.** See the table below.
- **Use one word for one concept, and always the same word.** Do not use a second word for
  the same thing. A second word makes the reader stop and check.
- **Explain a hard word the first time.** Write the word, then a plain meaning in brackets.
- **Drop the adverb** unless it changes the meaning: quite, very, quickly, easily,
  effectively.
- **No modifier stacks.** Do not put three adjectives in front of one noun. Rewrite the
  chain as a clause.

| Use this | Not this |
|---|---|
| use | utilize, make use of |
| remove | extract, take away, eliminate |
| tell | inform, let know |
| to | in order to, as a means to |
| also | in addition |
| connect | establish connectivity |
| because | since, where the meaning is cause |

## Punctuation and form

- **No contractions.** Write "do not", "it is", and "you will" in full. Full words are
  easier to read and easier to translate.
- **No em dash.** Use a comma, a colon, or brackets.
- **Sentence-style capitalization in headings.** Capitalize the first word and proper names
  only.
- **Keep the last comma** in a list of three or more items: "a, b, and c".
- **One space** after a period, a question mark, or a colon.

## What not to write

- **No idioms and no culture references.** They do not travel.
- **No jargon without a reason.** Where a technical term is the exact word, use it and
  explain it once.
- **No softening when the news is bad.** Write "this failed" and "I did not verify this" in
  plain words. Do not bury either one in a longer sentence.

## Test it

Read the sentence aloud. If you cannot say it in one breath, it is too long. Split it.

## Nami only

Four limits that exist because of this repository. Delete this section when you copy the
file elsewhere.

1. **Meaning outranks style, and the evidence rule owns the meaning.** The "Evidence rule"
   section of [`../../CLAUDE.md`](../../CLAUDE.md) wins over every rule above. Where a
   simpler sentence would weaken a claim, keep the claim and split the sentence.
2. **Never simplify a quotation.** Quoted outside text stays word for word, including its
   contractions and its spelling. Measured on 2026-08-04: all 8 contractions in tracked
   markdown sit inside quotations of outside material. The repository otherwise writes `do
   not` 150 times and `cannot` 457 times. A style pass must not "fix" any of the 8.
3. **Never simplify a dated measurement out of its date or its tense.**
   [`../../docs/CLAUDE.md`](../../docs/CLAUDE.md) states the rule: "A sentence asserting what
   another file *currently* contains is a measurement, so it is dated, written in the past
   tense, and names the commit it was true at." Shortening such a sentence usually deletes
   the date or moves it to the present tense, and either one stops it being evidence.
4. **Spelling variant is an open gap, not a rule.** The repository is mixed, and no document
   rules on it. Counted on 2026-08-04 over tracked markdown in `docs/` plus the `CLAUDE.md`
   files: `behaviour` 125 against `behavior` 104, and `licence` 211 against `license` 392.
   Many `license` hits are quoted SPDX text, file names, and licence names, which cannot
   change. Do not switch a spelling as part of a style edit. Do not treat either form as the
   house form.

## Two rules from the style guide that this repository does not take

Both are deliberate, so do not "correct" them back.

| The style guide says | This repository does | Why |
|---|---|---|
| Use an em dash, with no spaces around it | No em dash at all | The root `CLAUDE.md` forbids it, and Check 5 of `scripts/check-adrs.sh` fails the build on one. Use a comma, a colon, or brackets. |
| Use contractions, to sound friendly | Full forms only | Full words are easier to read for a reader whose first language is not English, and easier to translate. This also matches what the repository already does. |
