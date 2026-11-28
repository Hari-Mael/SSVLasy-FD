import pytest

from app.modules.activities.domain.ciphers import (
    CipherKind,
    CipherSpec,
    answer_key,
    encode_message,
    normalize_answer,
)
from app.shared.errors import AppError, ErrorCode


def encoded(plain: str, kind: CipherKind, shift: int | None = None, key: str | None = None) -> str:
    return encode_message(plain, CipherSpec(kind, shift, key)).encoded


def test_normalize_removes_accents_punctuation_and_extra_spaces() -> None:
    assert normalize_answer("  Été  à Paris! ") == "ETE A PARIS"


def test_morse_separates_letters_and_words() -> None:
    assert encoded("SOS sos", CipherKind.MORSE) == "... --- ... / ... --- ..."


def test_caesar_wraps_around_the_alphabet() -> None:
    assert encoded("abc xyz", CipherKind.CAESAR, shift=3) == "DEF ABC"


def test_caesar_requires_a_valid_shift() -> None:
    with pytest.raises(AppError) as error:
        encoded("abc", CipherKind.CAESAR, shift=0)
    assert error.value.code is ErrorCode.VALIDATION


def test_atbash_mirrors_the_alphabet() -> None:
    assert encoded("abc", CipherKind.ATBASH) == "ZYX"


def test_vigenere_matches_the_reference_example() -> None:
    assert encoded("attack at dawn", CipherKind.VIGENERE, key="lemon") == "LXFOPV EF RNHR"


def test_vigenere_requires_a_key() -> None:
    with pytest.raises(AppError):
        encoded("abc", CipherKind.VIGENERE)


def test_polybius_merges_i_and_j() -> None:
    assert encoded("hello", CipherKind.POLYBIUS) == "23 15 31 31 34"
    assert encoded("ij", CipherKind.POLYBIUS) == encoded("ii", CipherKind.POLYBIUS)


def test_polybius_ignores_digits_like_the_game() -> None:
    assert encoded("a1", CipherKind.POLYBIUS) == "11"


def test_a1z26_separates_numbers_and_words() -> None:
    assert encoded("abc de", CipherKind.A1Z26) == "1 2 3 / 4 5"


def test_reversed_flips_the_text() -> None:
    assert encoded("abc def", CipherKind.REVERSED) == "FED CBA"


def test_answer_key_ignores_case_accents_spaces_and_punctuation() -> None:
    assert answer_key("L'Été, à Paris") == "LETEAPARIS"
    assert answer_key("s.o.s") == answer_key("SOS")


def test_empty_message_is_refused() -> None:
    with pytest.raises(AppError):
        encode_message("!!!", CipherSpec(CipherKind.MORSE))


def test_otan_spells_each_letter() -> None:
    assert encoded("abc", CipherKind.OTAN) == "Alpha, Bravo, Charlie"


def test_semaphore_requires_a_naval_activity() -> None:
    with pytest.raises(AppError):
        encoded("ab", CipherKind.SEMAPHORE)
    assert (
        encode_message("ab", CipherSpec(CipherKind.SEMAPHORE, naval=True)).encoded == "SEM01 SEM02"
    )


def test_montre_murale_uses_the_clock_positions() -> None:
    assert encoded("am", CipherKind.MONTRE_MURALE) == "121 122"


def test_pyramide_uses_row_and_column() -> None:
    assert encoded("abc", CipherKind.PYRAMIDE) == "11-21-22"


def test_morse_lampe_and_sifflet_prefix_the_morse() -> None:
    assert encoded("sos", CipherKind.MORSE_LAMPE) == "LAMPE: ... --- ..."
    assert encoded("sos", CipherKind.MORSE_SIFFLET) == "SIFFLET: ... --- ..."


def test_pending_families_cannot_be_created_yet() -> None:
    with pytest.raises(AppError):
        encoded("abc", CipherKind.VAKANA)


def test_code_code_mentions_all_three_steps() -> None:
    result = encoded("ok", CipherKind.CODE_CODE)
    assert "OTAN" in result and "MORSE" in result and "VOANJO" in result
