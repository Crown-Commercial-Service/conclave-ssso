import { Pipe, PipeTransform } from "@angular/core";

@Pipe({
    name: 'enumToArray'
})

export class EnumToArrayPipe implements PipeTransform {
    transform(data: Object): number[] {
        let keys = Object.keys(data);
        let filteredKeys = keys.filter(k => !isNaN(Number(k)));
        return filteredKeys.map(k => parseInt(k));
    }
}