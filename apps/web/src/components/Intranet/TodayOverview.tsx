// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

import { useMemo } from 'react';
import { Repeat2 } from 'lucide-react';
import { useGetApiWeather } from '../../api/generated/endpoints';
import { useCalendarEventsQuery } from '../../hooks/useCalendarQueries';
import type { WeatherDto } from '@types';
import { useClock } from '../../hooks/useClock';
import { formatDateTime } from '../../utils/dateTime';
import { getEventDayTimingLabel, getEventEmoji } from './calendar/calendarPresentation';
import { getMockCalendarEvents } from './calendar/mockEvents';

const getWeatherEmoji = (code?: number | null) => {
  if (code === undefined || code === null) return '🌤️';
  if (code === 0) return '☀️';
  if (code <= 3) return '🌤️';
  if (code === 45 || code === 48) return '🌫️';
  if (code >= 51 && code <= 57) return '🌦️';
  if (code >= 61 && code <= 67) return '🌧️';
  if (code >= 71 && code <= 77) return '❄️';
  if (code >= 80 && code <= 82) return '🌧️';
  if (code >= 95) return '⛈️';
  return '🌤️';
};

type CompleteWeather = WeatherDto & Required<Pick<WeatherDto,
  'location' | 'temperature' | 'apparentTemperature' | 'precipitationProbability' | 'precipitation' | 'weatherCode'
>>;

const hasCompleteWeather = (weather?: WeatherDto): weather is CompleteWeather => Boolean(
  weather?.location
  && typeof weather.temperature === 'number'
  && typeof weather.apparentTemperature === 'number'
  && typeof weather.precipitationProbability === 'number'
  && typeof weather.precipitation === 'number'
  && typeof weather.weatherCode === 'number',
);

interface WeatherContentProps {
  isLoading: boolean;
  isError: boolean;
  weather?: WeatherDto;
}

function WeatherContent({ isLoading, isError, weather }: WeatherContentProps) {
  if (isLoading) {
    return (
      <div className="text-base md:text-lg font-bold text-primary-container" role="status">
        Loading weather…
      </div>
    );
  }

  if (isError) {
    return (
      <div role="alert">
        <span className="block text-base md:text-lg font-bold text-error-container">Weather unavailable</span>
        <span className="block text-base md:text-lg font-medium text-primary-container">API request failed</span>
      </div>
    );
  }

  if (!hasCompleteWeather(weather)) {
    return (
      <div role="alert">
        <span className="block text-base md:text-lg font-bold text-error-container">Weather unavailable</span>
        <span className="block text-base md:text-lg font-medium text-primary-container">Incomplete API response</span>
      </div>
    );
  }

  return (
    <>
      <span className="text-2xl md:text-3xl">
        {getWeatherEmoji(weather.weatherCode)}
      </span>
      <span className="text-2xl md:text-3xl font-bold mt-1">
        {Math.round(weather.temperature)}°C
      </span>
      <span className="text-base md:text-lg font-bold uppercase tracking-wide">
        {weather.location}
      </span>
      <span className="text-base md:text-lg font-medium text-primary-container">
        Feels {Math.round(weather.apparentTemperature)}° · {weather.precipitationProbability}% rain · {weather.precipitation.toFixed(1)} mm
      </span>
    </>
  );
}

export function TodayOverview() {
  const time = useClock();

  // Calculate start/end of today in UTC
  const todayRange = useMemo(() => {
    const start = new Date(time);
    start.setHours(0, 0, 0, 0);
    const end = new Date(time);
    end.setHours(23, 59, 59, 999);
    return {
      start: start.toISOString(),
      end: end.toISOString()
    };
  }, [time]);

  // Generate mock events for design verification
  const mockEvents = useMemo(() => getMockCalendarEvents(time), [time]);

  // Fetch today's events from the backend
  const { data: rawEvents = [], isLoading, isError: isEventsError } = useCalendarEventsQuery(todayRange.start, todayRange.end);
  const events = useMemo(
    () => [...rawEvents, ...mockEvents].sort((a, b) => new Date(a.startTime ?? 0).getTime() - new Date(b.startTime ?? 0).getTime()),
    [rawEvents, mockEvents],
  );

  // Fetch current weather
  const { data: weatherData, isLoading: isWeatherLoading, isError: isWeatherError } = useGetApiWeather();
  const weather = weatherData?.data;

  const dateString = formatDateTime(time, { weekday: 'long', month: 'long', day: 'numeric' }, 'en-SE');
  const timeString = formatDateTime(time, { hour: '2-digit', minute: '2-digit', hour12: false }, 'en-SE');

  return (
    <section className="gap-6 md:gap-8 rounded-2xl border-0 overflow-hidden bg-brand-bg-secondary text-primary-container h-[400px] max-h-[400px] md:max-h-none md:h-full relative flex flex-col w-full min-w-0 p-6">
      <div className="relative z-10 flex flex-wrap justify-between items-start gap-6 shrink-0">
        <div className="flex flex-col min-w-[150px]">
          <span className="text-base md:text-lg font-black text-brand-accent uppercase tracking-wide mb-1">{dateString}</span>
          <span className="text-4xl md:text-5xl lg:text-6xl font-black tracking-tighter leading-none">{timeString}</span>
        </div>
        
        <div className="flex flex-1 flex-col sm:items-end text-right shrink-0">
          <WeatherContent 
            isLoading={isWeatherLoading}
            isError={isWeatherError}
            weather={weather}
          />
        </div>
      </div>

      {/* Event list */}
      <div className="relative z-10 flex flex-col flex-1 min-h-0 w-full min-w-0">
        <div className="flex items-center justify-between pb-2 mb-1">
          <h3 className="text-base md:text-lg font-bold text-primary-container">Today's Schedule</h3>
          <div className="flex items-center gap-2">
            <span className="text-sm md:text-base font-bold text-primary-container">{events.length} Events</span>
          </div>
        </div>

        <div className="flex flex-col overflow-y-auto flex-1 divide-y divide-slate-50/30 custom-scrollbar w-full min-w-0">
          {isLoading ? (
            <div className="text-sm md:text-base text-primary-container italic text-center py-2">Loading today's schedule...</div>
          ) : isEventsError ? (
            <div role="alert" className="py-2 text-center">
              <span className="block text-sm md:text-base font-bold text-error-container">Schedule unavailable</span>
              <span className="block text-sm md:text-base text-primary-container">Calendar request failed</span>
            </div>
          ) : events.length === 0 ? (
            <div className="text-sm md:text-base text-primary-container italic text-center py-2">No events scheduled for today.</div>
          ) : (
            events.map(e => (
              <div 
                key={`${e.id}-${e.startTime}`}
                className="flex flex-col transition-colors py-4 justify-center w-full min-w-0 shrink-0 gap-1"
              >
                {/* Row 1: icon, time, title */}
                <div className="flex items-center gap-1 min-w-0 flex-shrink-0">
                  <span className="text-sm md:text-base shrink-0 leading-none pr-1">{getEventEmoji(e.eventType)}</span>
                  <span className="text-sm md:text-base font-black text-brand-accent whitespace-nowrap leading-none shrink-0">
                    {getEventDayTimingLabel(e, time)}
                  </span>
                  <div className="flex items-center gap-4 min-w-0 flex-1">
                    <span className="text-base md:text-lg font-bold text-primary-container truncate leading-none flex-1 min-w-0">{e.title}</span>
                    {e.isRecurring && (
                      <span className="inline-flex shrink-0 items-center gap-1 text-sm font-bold text-primary-container">
                        <Repeat2 size={13} aria-hidden="true" />
                        {e.recurrence && e.recurrence !== 'None' ? e.recurrence : 'Recurring'}
                      </span>
                    )}
                    {e.location && (
                      <span className="text-sm md:text-base text-primary-container font-bold uppercase tracking-wider truncate leading-none max-w-[50%] min-w-0">
                        📍 {e.location}
                      </span>
                    )}
                  </div>
                </div>

                {/* Row 2: content / description */}
                <div className="text-sm md:text-base text-primary-container truncate min-w-0 leading-normal select-text flex-shrink-0 ml-[22px]">
                  {e.description || 'No description provided.'}
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </section>
  );
}
