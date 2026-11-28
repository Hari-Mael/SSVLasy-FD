import itertools
import string
import unicodedata
from collections.abc import Callable
from dataclasses import dataclass
from enum import StrEnum
from typing import Final

from app.modules.activities.domain.game_tables import (
    CHIFFRE_TABLE,
    CODE_Q_TABLE,
    LOHATENY_TABLE,
    MORSE_TABLE,
    OTAN_TABLE,
)
from app.shared.errors import AppError

LETTERS: Final = string.ascii_uppercase
WORD_SEPARATOR: Final = " / "
POLYBIUS_LETTERS: Final = LETTERS.replace("J", "")
POLYBIUS_SIZE: Final = 5
MIN_SHIFT: Final = 1
MAX_SHIFT: Final = 25
MORSE: Final[dict[str, str]] = MORSE_TABLE
PYRAMID_ROWS: Final[tuple[str, ...]] = (
    "A",
    "BC",
    "DEF",
    "GHIJ",
    "KLMNO",
    "PQRSTU",
    "VWXYZ12",
    "34567890",
)
MONTRE_MURALE: Final[dict[str, str]] = {
    "A": "121",
    "M": "122",
    "Y": "123",
    "J": "91",
    "V": "92",
    "Q": "42",
}


class CipherKind(StrEnum):
    # Codes implémentés (mêmes règles que le jeu)
    MORSE = "morse"
    MORSE_LAMPE = "morse_lampe"
    MORSE_SIFFLET = "morse_sifflet"
    CAESAR = "caesar"
    ATBASH = "atbash"
    VIGENERE = "vigenere"
    POLYBIUS = "polybius"
    A1Z26 = "a1z26"
    REVERSED = "reversed"
    OTAN = "otan"
    SEMAPHORE = "semaphore"
    MONTRE_MURALE = "montre_murale"
    PYRAMIDE = "pyramide"
    CODE_Q = "code_q"
    CHIFFRE = "chiffre"
    LOHATENY = "lohateny"
    CODE_CODE = "code_code"
    # Familles du cahier dont les règles restent à fournir : catalogue, non encore créables
    VAKANA = "vakana"
    RANTSAN_KAZO = "rantsan_kazo"
    ARABIE = "arabie"
    VOANJO = "voanjo"
    DAN = "dan"
    KELY = "kely"
    CODE_INDIENNE = "code_indienne"
    CODES_TABLEAUX = "codes_tableaux"
    CODE_TEXTE = "code_texte"
    WOSM = "wosm"
    PIGPEN = "pigpen"
    BRAILLE = "braille"
    SIGNES_PISTE = "signes_piste"
    AZIMUTS = "azimuts"
    NOEUDS = "noeuds"
    EMPREINTES = "empreintes"


PENDING_KINDS: Final[frozenset[CipherKind]] = frozenset(
    {
        CipherKind.VAKANA,
        CipherKind.RANTSAN_KAZO,
        CipherKind.ARABIE,
        CipherKind.VOANJO,
        CipherKind.DAN,
        CipherKind.KELY,
        CipherKind.CODE_INDIENNE,
        CipherKind.CODES_TABLEAUX,
        CipherKind.CODE_TEXTE,
        CipherKind.WOSM,
        CipherKind.PIGPEN,
        CipherKind.BRAILLE,
        CipherKind.SIGNES_PISTE,
        CipherKind.AZIMUTS,
        CipherKind.NOEUDS,
        CipherKind.EMPREINTES,
    }
)


@dataclass(frozen=True, slots=True)
class CipherSpec:
    kind: CipherKind
    shift: int | None = None
    key: str | None = None
    naval: bool = False


@dataclass(frozen=True, slots=True)
class EncodedMessage:
    cipher: CipherSpec
    encoded: str


def normalize_answer(text: str) -> str:
    decomposed = unicodedata.normalize("NFKD", text)
    stripped = "".join(char for char in decomposed if not unicodedata.combining(char))
    kept = "".join(char if char.isascii() and char.isalnum() else " " for char in stripped.upper())
    return " ".join(kept.split())


def answer_key(text: str) -> str:
    return normalize_answer(text).replace(" ", "")


def _checked_shift(spec: CipherSpec) -> int:
    if spec.shift is None or not MIN_SHIFT <= spec.shift <= MAX_SHIFT:
        raise AppError.validation(f"Le décalage doit être compris entre {MIN_SHIFT} et {MAX_SHIFT}")
    return spec.shift


def _checked_key(spec: CipherSpec) -> str:
    key = answer_key(spec.key or "")
    if not key or not key.isalpha():
        raise AppError.validation("La clé doit contenir uniquement des lettres")
    return key


def validate_spec(spec: CipherSpec) -> CipherSpec:
    if spec.kind in PENDING_KINDS:
        raise AppError.validation(
            f"Le code {spec.kind.value} n'est pas encore défini : ses règles doivent être fournies"
        )
    if spec.kind is CipherKind.SEMAPHORE and not spec.naval:
        raise AppError.validation("Le sémaphore n'est autorisé que pour une activité navale")
    if spec.kind is CipherKind.CAESAR:
        return CipherSpec(spec.kind, shift=_checked_shift(spec), naval=spec.naval)
    if spec.kind is CipherKind.VIGENERE:
        return CipherSpec(spec.kind, key=_checked_key(spec), naval=spec.naval)
    return CipherSpec(spec.kind, naval=spec.naval)


def _words(text: str) -> list[str]:
    return [word for word in text.split(" ") if word]


def _rotate(char: str, offset: int) -> str:
    return LETTERS[(LETTERS.index(char) + offset) % len(LETTERS)]


def _morse_words(text: str) -> str:
    return WORD_SEPARATOR.join(
        " ".join(MORSE[char] for char in word if char in MORSE) for word in _words(text)
    )


def _polybius_pair(char: str) -> str:
    index = POLYBIUS_LETTERS.index("I" if char == "J" else char)
    return f"{index // POLYBIUS_SIZE + 1}{index % POLYBIUS_SIZE + 1}"


def _inverse_table(table: dict[str, str]) -> dict[str, str]:
    return {normalize_answer(value): key for key, value in table.items()}


def _lookup_words(text: str, table: dict[str, str]) -> str:
    inverse = _inverse_table(table)
    if text in inverse:
        return inverse[text]
    return " | ".join(inverse.get(word, "[?]") for word in _words(text))


def _lookup_characters(text: str, table: dict[str, str]) -> str:
    inverse = _inverse_table(table)
    return " ".join(inverse.get(char, "[?]") for char in text if char != " ")


def _otan(text: str) -> str:
    return ", ".join(OTAN_TABLE[char] for char in text if char in OTAN_TABLE)


def _encode_morse(text: str, spec: CipherSpec) -> str:
    return _morse_words(text)


def _encode_morse_lampe(text: str, spec: CipherSpec) -> str:
    return "LAMPE: " + _morse_words(text)


def _encode_morse_sifflet(text: str, spec: CipherSpec) -> str:
    return "SIFFLET: " + _morse_words(text)


def _encode_caesar(text: str, spec: CipherSpec) -> str:
    shift = _checked_shift(spec)
    return "".join(_rotate(char, shift) if char in LETTERS else char for char in text)


def _encode_atbash(text: str, spec: CipherSpec) -> str:
    return "".join(
        LETTERS[len(LETTERS) - 1 - LETTERS.index(char)] if char in LETTERS else char
        for char in text
    )


def _encode_vigenere(text: str, spec: CipherSpec) -> str:
    offsets = itertools.cycle(LETTERS.index(char) for char in _checked_key(spec))
    return "".join(_rotate(char, next(offsets)) if char in LETTERS else char for char in text)


def _encode_polybius(text: str, spec: CipherSpec) -> str:
    return WORD_SEPARATOR.join(
        " ".join(_polybius_pair(char) for char in word if char in POLYBIUS_LETTERS or char == "J")
        for word in _words(text)
    )


def _encode_a1z26(text: str, spec: CipherSpec) -> str:
    return WORD_SEPARATOR.join(
        " ".join(str(LETTERS.index(char) + 1) for char in word if char in LETTERS)
        for word in _words(text)
    )


def _encode_reversed(text: str, spec: CipherSpec) -> str:
    return text[::-1]


def _encode_otan(text: str, spec: CipherSpec) -> str:
    return _otan(text)


def _encode_semaphore(text: str, spec: CipherSpec) -> str:
    return " ".join(f"SEM{LETTERS.index(char) + 1:02d}" for char in text if char in LETTERS)


def _encode_montre_murale(text: str, spec: CipherSpec) -> str:
    return " ".join(MONTRE_MURALE.get(char, "?") for char in text)


def _encode_pyramide(text: str, spec: CipherSpec) -> str:
    positions: dict[str, str] = {}
    for row_index, row in enumerate(PYRAMID_ROWS):
        for column_index, char in enumerate(row):
            positions[char] = f"{row_index + 1}{column_index + 1}"
    parts = [
        "/" if char == " " else positions[char] for char in text if char == " " or char in positions
    ]
    return "-".join(parts)


def _encode_code_q(text: str, spec: CipherSpec) -> str:
    return _lookup_words(text, CODE_Q_TABLE)


def _encode_chiffre(text: str, spec: CipherSpec) -> str:
    return _lookup_words(text, CHIFFRE_TABLE)


def _encode_lohateny(text: str, spec: CipherSpec) -> str:
    return _lookup_characters(text, LOHATENY_TABLE)


def _encode_code_code(text: str, spec: CipherSpec) -> str:
    return (
        "DINGANA 1 OTAN / ÉTAPE 1 OTAN: "
        + _otan(text)
        + " → DINGANA 2 MORSE / ÉTAPE 2 MORSE: "
        + _morse_words(text)
        + " → DINGANA 3 VOANJO / ÉTAPE 3 VOANJO: jereo p.30 / voir p.30"
    )


ENCODERS: Final[dict[CipherKind, Callable[[str, CipherSpec], str]]] = {
    CipherKind.MORSE: _encode_morse,
    CipherKind.MORSE_LAMPE: _encode_morse_lampe,
    CipherKind.MORSE_SIFFLET: _encode_morse_sifflet,
    CipherKind.CAESAR: _encode_caesar,
    CipherKind.ATBASH: _encode_atbash,
    CipherKind.VIGENERE: _encode_vigenere,
    CipherKind.POLYBIUS: _encode_polybius,
    CipherKind.A1Z26: _encode_a1z26,
    CipherKind.REVERSED: _encode_reversed,
    CipherKind.OTAN: _encode_otan,
    CipherKind.SEMAPHORE: _encode_semaphore,
    CipherKind.MONTRE_MURALE: _encode_montre_murale,
    CipherKind.PYRAMIDE: _encode_pyramide,
    CipherKind.CODE_Q: _encode_code_q,
    CipherKind.CHIFFRE: _encode_chiffre,
    CipherKind.LOHATENY: _encode_lohateny,
    CipherKind.CODE_CODE: _encode_code_code,
}


def encode_message(plain: str, spec: CipherSpec) -> EncodedMessage:
    checked = validate_spec(spec)
    normalized = normalize_answer(plain)
    if not normalized:
        raise AppError.validation("Le message est vide")
    return EncodedMessage(checked, ENCODERS[checked.kind](normalized, checked))
