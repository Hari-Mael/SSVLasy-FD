from datetime import date, datetime, time, timedelta
from zoneinfo import ZoneInfo


def slot_window(
    start_date: date, timezone: str, day: int, begins: time, ends: time
) -> tuple[datetime, datetime]:
    zone = ZoneInfo(timezone)
    day_date = start_date + timedelta(days=day - 1)
    return datetime.combine(day_date, begins, zone), datetime.combine(day_date, ends, zone)


def slot_is_open(
    start_date: date, timezone: str, day: int, begins: time, ends: time, instant: datetime
) -> bool:
    opening, closing = slot_window(start_date, timezone, day, begins, ends)
    return opening <= instant <= closing
